using Hangfire;
using LearningPlatform.Application;
using LearningPlatform.Domain;
using LearningPlatform.Infrastructure.Repositories;

namespace LearningPlatform.Application.Services;

public class LearningService(
    ICurriculumRepository curriculumRepository,
    IChildRepository childRepository,
    ILearningRepository learningRepository,
    INotificationRepository notificationRepository,
    IUnitOfWork unitOfWork,
    IBackgroundJobClient backgroundJobs,
    IParentNotificationPublisher notificationPublisher)
{
    public async Task<ExerciseSubmitResponse> SubmitExercise(Guid exerciseId, SubmitExerciseRequest request)
    {
        var exercise = await curriculumRepository.GetExercise(exerciseId)
                       ?? throw new KeyNotFoundException("Exercise not found.");
        if (exercise.Lesson is null || !exercise.Lesson.IsPublished || exercise.Lesson.Unit is null || !exercise.Lesson.Unit.IsPublished)
            throw new KeyNotFoundException("Exercise not found.");
        var child = await childRepository.GetById(request.ChildId) ?? throw new KeyNotFoundException("Child not found.");

        await EnsureChildLessonInChildProgram(child, exercise.LessonId);
        await EnsurePreviousUnitsAllLessonsCompleted(child, exercise.LessonId);

        var lessonId = exercise.LessonId;
        if (await learningRepository.CountExercises(lessonId) == 0)
            throw new InvalidOperationException("This lesson has no exercises.");

        if (await learningRepository.AllLessonExercisesHaveCorrectAnswerAsync(child.Id, lessonId))
        {
            var (_, unitDone, signal) = await TryFinalizeLessonIfNeeded(child, lessonId);
            await unitOfWork.SaveChanges();
            await PublishNotificationsAsync(signal);
            EnqueueUnitCompletionFollowUp(child.Id, exercise.Lesson.UnitId, unitDone);
            throw new InvalidOperationException("Lesson already completed.");
        }

        var nextRequired = await GetNextExerciseIdRequiringFirstCorrect(child.Id, lessonId);
        if (nextRequired is null)
        {
            var (_, unitDone, signal) = await TryFinalizeLessonIfNeeded(child, lessonId);
            await unitOfWork.SaveChanges();
            await PublishNotificationsAsync(signal);
            EnqueueUnitCompletionFollowUp(child.Id, exercise.Lesson.UnitId, unitDone);
            throw new InvalidOperationException("Lesson already completed.");
        }

        if (exerciseId != nextRequired.Value)
            throw new InvalidOperationException("Complete the current exercise in order before moving on.");

        var hadCorrectBefore = await learningRepository.HasCorrectAnswerAsync(request.ChildId, exerciseId);

        var result = new ExerciseResult
        {
            ChildId = request.ChildId,
            ExerciseId = exerciseId,
            IsCorrect = request.IsCorrect,
            TimeTakenMs = request.TimeTakenMs
        };
        await learningRepository.AddExerciseResult(result);

        var unitId = exercise.Lesson.UnitId;
        if (!request.IsCorrect && !hadCorrectBefore)
        {
            var unitProgress = await learningRepository.GetOrCreateChildUnitProgress(request.ChildId, unitId);
            unitProgress.ErrorCount += 1;
        }

        var progress = await learningRepository.GetProgress(request.ChildId, lessonId);
        if (progress is null)
        {
            progress = new ChildLessonProgress
            {
                ChildId = request.ChildId,
                LessonId = lessonId,
                Status = LessonProgressStatus.InProgress
            };
            await learningRepository.AddProgress(progress);
        }

        if (request.IsCorrect && !hadCorrectBefore)
            progress.LastCompletedExerciseId = exerciseId;

        await unitOfWork.SaveChanges();

        var lessonJustCompleted = false;
        var unitNewlyCompleted = false;
        List<Notification> lessonSignal = [];
        if (await learningRepository.AllLessonExercisesHaveCorrectAnswerAsync(child.Id, lessonId))
        {
            var r = await TryFinalizeLessonIfNeeded(child, lessonId);
            lessonJustCompleted = r.LessonFinalized;
            unitNewlyCompleted = r.UnitNewlyCompleted;
            lessonSignal = r.Notifications;
        }

        await unitOfWork.SaveChanges();
        await PublishNotificationsAsync(lessonSignal);
        EnqueueUnitCompletionFollowUp(child.Id, exercise.Lesson.UnitId, unitNewlyCompleted);

        return new ExerciseSubmitResponse(result.Id, result.IsCorrect, result.TimeTakenMs, result.SubmittedAt, lessonJustCompleted);
    }

    public Task<LessonResumeResponse> GetLessonResume(Guid lessonId, Guid childId) => BuildLessonResume(childId, lessonId);

    public async Task<object> CompleteLesson(Guid lessonId, Guid childId)
    {
        var lesson = await curriculumRepository.GetLesson(lessonId);
        if (lesson is null || !lesson.IsPublished || lesson.Unit is null || !lesson.Unit.IsPublished)
            throw new KeyNotFoundException("Lesson not found.");
        var child = await childRepository.GetById(childId) ?? throw new KeyNotFoundException("Child not found.");

        await EnsureChildLessonInChildProgram(child, lessonId);
        await EnsurePreviousUnitsAllLessonsCompleted(child, lessonId);

        if (await learningRepository.CountExercises(lessonId) == 0)
            throw new InvalidOperationException("This lesson has no exercises.");

        if (!await learningRepository.AllLessonExercisesHaveCorrectAnswerAsync(childId, lessonId))
            throw new InvalidOperationException("Answer all exercises correctly first.");

        var (_, unitDone, signal) = await TryFinalizeLessonIfNeeded(child, lessonId);
        await unitOfWork.SaveChanges();
        await PublishNotificationsAsync(signal);
        EnqueueUnitCompletionFollowUp(childId, lesson.UnitId, unitDone);

        child = await childRepository.GetById(childId) ?? throw new KeyNotFoundException("Child not found.");
        return new { ChildId = childId, child!.XpTotal, child.CurrentLevel, child.StreakCurrent };
    }

    private async Task<LessonResumeResponse> BuildLessonResume(Guid childId, Guid lessonId)
    {
        var lesson = await curriculumRepository.GetLesson(lessonId);
        if (lesson is null || !lesson.IsPublished || lesson.Unit is null || !lesson.Unit.IsPublished)
            throw new KeyNotFoundException("Lesson not found.");
        var child = await childRepository.GetById(childId) ?? throw new KeyNotFoundException("Child not found.");

        await EnsureChildLessonInChildProgram(child, lessonId);
        await EnsurePreviousUnitsAllLessonsCompleted(child, lessonId);

        if (await learningRepository.CountExercises(lessonId) == 0)
            throw new InvalidOperationException("This lesson has no exercises.");

        var (_, unitDone, signal) = await TryFinalizeLessonIfNeeded(child, lessonId);
        await unitOfWork.SaveChanges();
        await PublishNotificationsAsync(signal);
        EnqueueUnitCompletionFollowUp(childId, lesson.UnitId, unitDone);

        var progress = await learningRepository.GetProgress(childId, lessonId);
        var allCorrect = await learningRepository.AllLessonExercisesHaveCorrectAnswerAsync(childId, lessonId);
        var completed = progress?.Status == LessonProgressStatus.Completed || allCorrect;

        Guid? next = null;
        if (!completed)
            next = await GetNextExerciseIdRequiringFirstCorrect(childId, lessonId);

        return new LessonResumeResponse(next, completed);
    }

    /// <returns>Завершение урока впервые; юнит впервые завершён; уведомления для SignalR после SaveChanges.</returns>
    private async Task<(bool LessonFinalized, bool UnitNewlyCompleted, List<Notification> Notifications)> TryFinalizeLessonIfNeeded(Child child, Guid lessonId)
    {
        if (!await learningRepository.AllLessonExercisesHaveCorrectAnswerAsync(child.Id, lessonId))
            return (false, false, []);

        var lesson = await curriculumRepository.GetLesson(lessonId) ?? throw new KeyNotFoundException("Lesson not found.");
        var progress = await learningRepository.GetProgress(child.Id, lessonId);
        if (progress is null)
        {
            progress = new ChildLessonProgress { ChildId = child.Id, LessonId = lessonId };
            await learningRepository.AddProgress(progress);
        }

        if (progress.Status == LessonProgressStatus.Completed)
            return (false, false, []);

        progress.Status = LessonProgressStatus.Completed;
        progress.XpAwarded = lesson.XpReward;
        progress.CompletedAt = DateTime.UtcNow;

        var xpBefore = child.XpTotal;
        child.XpTotal += lesson.XpReward;
        var oldLevel = CalculateLevel(xpBefore);
        var newLevel = CalculateLevel(child.XpTotal);
        child.CurrentLevel = newLevel;
        UpdateStreak(child);

        var signal = new List<Notification>();

        var lessonNotification = new Notification
        {
            ParentId = child.ParentId,
            ChildId = child.Id,
            Type = NotificationType.Milestone,
            Title = "Lesson completed",
            Body = $"{child.DisplayName} completed {lesson.Title} and earned {lesson.XpReward} XP."
        };
        await notificationRepository.Add(lessonNotification);
        signal.Add(lessonNotification);

        if (newLevel > oldLevel)
        {
            var levelNotification = new Notification
            {
                ParentId = child.ParentId,
                ChildId = child.Id,
                Type = NotificationType.LevelUp,
                Title = "Level up",
                Body = $"{child.DisplayName} reached level {newLevel}."
            };
            await notificationRepository.Add(levelNotification);
            signal.Add(levelNotification);
        }

        var unitNew = await TryMarkChildUnitCompletedIfNeeded(child, lesson.UnitId);
        if (unitNew)
        {
            var unit = await curriculumRepository.GetUnit(lesson.UnitId);
            var unitTitle = unit?.Title ?? "Unit";
            var unitNotification = new Notification
            {
                ParentId = child.ParentId,
                ChildId = child.Id,
                Type = NotificationType.UnitCompleted,
                Title = "Unit completed",
                Body = $"{child.DisplayName} completed all lessons in unit \"{unitTitle}\"."
            };
            await notificationRepository.Add(unitNotification);
            signal.Add(unitNotification);
        }

        return (true, unitNew, signal);
    }

    /// <summary>C1: уведомления уже в БД после <see cref="IUnitOfWork.SaveChanges"/> у вызывающего кода.</summary>
    private async Task PublishNotificationsAsync(IReadOnlyList<Notification> notifications)
    {
        foreach (var n in notifications)
            await notificationPublisher.PublishSavedAsync(n);
    }

    private void EnqueueUnitCompletionFollowUp(Guid childId, Guid unitId, bool unitNewlyCompleted)
    {
        if (!unitNewlyCompleted)
            return;
        backgroundJobs.Enqueue<UnitCompletionFollowUpJob>(j => j.RunAsync(childId, unitId));
    }

    /// <summary>Когда все опубликованные уроки юнита завершены — фиксируем статус прохождения юнита (E1).</summary>
    private async Task<bool> TryMarkChildUnitCompletedIfNeeded(Child child, Guid unitId)
    {
        var totalPublished = await curriculumRepository.CountPublishedLessonsInUnit(unitId);
        if (totalPublished == 0)
            return false;

        var completed = await learningRepository.CountCompletedPublishedLessonsInUnitAsync(child.Id, unitId);
        if (completed < totalPublished)
            return false;

        var unitProgress = await learningRepository.GetOrCreateChildUnitProgress(child.Id, unitId);
        if (unitProgress.Status == UnitProgressStatus.Completed)
            return false;

        unitProgress.Status = UnitProgressStatus.Completed;
        unitProgress.CompletedAt = DateTime.UtcNow;
        return true;
    }

    private async Task<Guid?> GetNextExerciseIdRequiringFirstCorrect(Guid childId, Guid lessonId)
    {
        var ids = await learningRepository.GetLessonExerciseIds(lessonId);
        foreach (var exId in ids)
        {
            if (!await learningRepository.HasCorrectAnswerAsync(childId, exId))
                return exId;
        }

        return null;
    }

    private async Task EnsureChildLessonInChildProgram(Child child, Guid lessonId)
    {
        var lesson = await curriculumRepository.GetLesson(lessonId) ?? throw new KeyNotFoundException("Lesson not found.");
        if (lesson.Unit is null || lesson.Unit.ProgramId != child.CurrentProgramId)
            throw new AppForbiddenException("This lesson is not in the child's current program.");
    }

    /// <summary>Следующий юнит программы доступен только когда во всех предыдущих юнитах завершены все уроки.</summary>
    private async Task EnsurePreviousUnitsAllLessonsCompleted(Child child, Guid lessonId)
    {
        var lesson = await curriculumRepository.GetLesson(lessonId) ?? throw new KeyNotFoundException("Lesson not found.");
        var unit = lesson.Unit ?? throw new InvalidOperationException("Lesson has no unit.");
        if (unit.ProgramId != child.CurrentProgramId)
            throw new AppForbiddenException("This lesson is not in the child's current program.");

        var unitsPage = await curriculumRepository.GetUnits(new UnitQueryOptions { ProgramId = unit.ProgramId, Page = 1, PageSize = 500 },
            restrictToPublishedCatalog: true);
        var earlierUnits = unitsPage.Items.Where(u => u.OrderIndex < unit.OrderIndex).OrderBy(u => u.OrderIndex).ToList();

        foreach (var u in earlierUnits)
        {
            var lessonsInUnit = await curriculumRepository.GetLessons(
                new LessonQueryOptions { UnitId = u.Id, ProgramId = unit.ProgramId, Page = 1, PageSize = 500 },
                restrictToPublishedCatalog: true);
            foreach (var les in lessonsInUnit.Items)
            {
                var p = await learningRepository.GetProgress(child.Id, les.Id);
                if (p?.Status != LessonProgressStatus.Completed)
                    throw new InvalidOperationException("Complete all lessons in previous units first.");
            }
        }
    }

    private static int CalculateLevel(int xp)
    {
        if (xp < 40) return 1;
        if (xp < 90) return 2;
        var n = 3;
        while (xp >= n * n * 10) n++;
        return n - 1;
    }

    private static void UpdateStreak(Child child)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (child.LastActivityDate is null)
        {
            child.StreakCurrent = 1;
        }
        else
        {
            var diff = today.DayNumber - child.LastActivityDate.Value.DayNumber;
            if (diff == 1) child.StreakCurrent += 1;
            else if (diff > 1) child.StreakCurrent = 1;
        }

        child.LastActivityDate = today;
        if (child.StreakCurrent > child.StreakLongest)
            child.StreakLongest = child.StreakCurrent;
    }
}
