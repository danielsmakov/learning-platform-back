using LearningPlatform.Application;
using LearningPlatform.Domain;
using LearningPlatform.Infrastructure.Repositories;

namespace LearningPlatform.Application.Services;

public class LearningService(
    ICurriculumRepository curriculumRepository,
    IChildRepository childRepository,
    ILearningRepository learningRepository,
    INotificationRepository notificationRepository,
    IUnitOfWork unitOfWork)
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
            await TryFinalizeLessonIfNeeded(child, lessonId);
            await unitOfWork.SaveChanges();
            throw new InvalidOperationException("Lesson already completed.");
        }

        var nextRequired = await GetNextExerciseIdRequiringFirstCorrect(child.Id, lessonId);
        if (nextRequired is null)
        {
            await TryFinalizeLessonIfNeeded(child, lessonId);
            await unitOfWork.SaveChanges();
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
        if (await learningRepository.AllLessonExercisesHaveCorrectAnswerAsync(child.Id, lessonId))
            lessonJustCompleted = await TryFinalizeLessonIfNeeded(child, lessonId);

        await unitOfWork.SaveChanges();

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

        await TryFinalizeLessonIfNeeded(child, lessonId);
        await unitOfWork.SaveChanges();

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

        await TryFinalizeLessonIfNeeded(child, lessonId);
        await unitOfWork.SaveChanges();

        var progress = await learningRepository.GetProgress(childId, lessonId);
        var allCorrect = await learningRepository.AllLessonExercisesHaveCorrectAnswerAsync(childId, lessonId);
        var completed = progress?.Status == LessonProgressStatus.Completed || allCorrect;

        Guid? next = null;
        if (!completed)
            next = await GetNextExerciseIdRequiringFirstCorrect(childId, lessonId);

        return new LessonResumeResponse(next, completed);
    }

    /// <returns><see langword="true"/> если урок впервые переведён в завершённый и начислен XP.</returns>
    private async Task<bool> TryFinalizeLessonIfNeeded(Child child, Guid lessonId)
    {
        if (!await learningRepository.AllLessonExercisesHaveCorrectAnswerAsync(child.Id, lessonId))
            return false;

        var lesson = await curriculumRepository.GetLesson(lessonId) ?? throw new KeyNotFoundException("Lesson not found.");
        var progress = await learningRepository.GetProgress(child.Id, lessonId);
        if (progress is null)
        {
            progress = new ChildLessonProgress { ChildId = child.Id, LessonId = lessonId };
            await learningRepository.AddProgress(progress);
        }

        if (progress.Status == LessonProgressStatus.Completed)
            return false;

        progress.Status = LessonProgressStatus.Completed;
        progress.XpAwarded = lesson.XpReward;
        progress.CompletedAt = DateTime.UtcNow;
        child.XpTotal += lesson.XpReward;
        child.CurrentLevel = CalculateLevel(child.XpTotal);
        UpdateStreak(child);

        var notification = new Notification
        {
            ParentId = child.ParentId,
            ChildId = child.Id,
            Type = NotificationType.Milestone,
            Title = "Lesson completed",
            Body = $"{child.DisplayName} completed {lesson.Title} and earned {lesson.XpReward} XP."
        };
        await notificationRepository.Add(notification);

        await TryMarkChildUnitCompletedIfNeeded(child, lesson.UnitId);

        return true;
    }

    /// <summary>Когда все опубликованные уроки юнита завершены — фиксируем статус прохождения юнита (E1).</summary>
    private async Task TryMarkChildUnitCompletedIfNeeded(Child child, Guid unitId)
    {
        var totalPublished = await curriculumRepository.CountPublishedLessonsInUnit(unitId);
        if (totalPublished == 0)
            return;

        var completed = await learningRepository.CountCompletedPublishedLessonsInUnitAsync(child.Id, unitId);
        if (completed < totalPublished)
            return;

        var unitProgress = await learningRepository.GetOrCreateChildUnitProgress(child.Id, unitId);
        if (unitProgress.Status == UnitProgressStatus.Completed)
            return;

        unitProgress.Status = UnitProgressStatus.Completed;
        unitProgress.CompletedAt = DateTime.UtcNow;
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
            throw new UnauthorizedAccessException("This lesson is not in the child's current program.");
    }

    /// <summary>Следующий юнит программы доступен только когда во всех предыдущих юнитах завершены все уроки.</summary>
    private async Task EnsurePreviousUnitsAllLessonsCompleted(Child child, Guid lessonId)
    {
        var lesson = await curriculumRepository.GetLesson(lessonId) ?? throw new KeyNotFoundException("Lesson not found.");
        var unit = lesson.Unit ?? throw new InvalidOperationException("Lesson has no unit.");
        if (unit.ProgramId != child.CurrentProgramId)
            throw new UnauthorizedAccessException("This lesson is not in the child's current program.");

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
