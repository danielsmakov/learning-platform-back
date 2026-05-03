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
    public async Task<object> SubmitExercise(Guid exerciseId, SubmitExerciseRequest request)
    {
        var exercise = await curriculumRepository.GetExercise(exerciseId)
                       ?? throw new KeyNotFoundException("Exercise not found.");
        var child = await childRepository.GetById(request.ChildId) ?? throw new KeyNotFoundException("Child not found.");

        await EnsureSequentialAccess(child.Id, exercise.LessonId);

        var result = new ExerciseResult
        {
            ChildId = request.ChildId,
            ExerciseId = exerciseId,
            IsCorrect = request.IsCorrect,
            TimeTakenMs = request.TimeTakenMs
        };
        await learningRepository.AddExerciseResult(result);

        var progress = await learningRepository.GetProgress(request.ChildId, exercise.LessonId);
        if (progress is null)
        {
            progress = new ChildLessonProgress
            {
                ChildId = request.ChildId,
                LessonId = exercise.LessonId,
                Status = LessonProgressStatus.InProgress
            };
            await learningRepository.AddProgress(progress);
        }

        await unitOfWork.SaveChanges();
        return new { result.Id, result.IsCorrect, result.TimeTakenMs, result.SubmittedAt };
    }

    public async Task<object> CompleteLesson(Guid lessonId, Guid childId)
    {
        var lesson = await curriculumRepository.GetLesson(lessonId);
        if (lesson is null || !lesson.IsPublished)
            throw new KeyNotFoundException("Lesson not found.");
        var child = await childRepository.GetById(childId) ?? throw new KeyNotFoundException("Child not found.");

        var lessonExerciseIds = await learningRepository.GetLessonExerciseIds(lessonId);
        var exercisesCount = lessonExerciseIds.Count;
        var submittedCount = await learningRepository.CountDistinctSubmitted(childId, lessonExerciseIds);
        if (submittedCount < exercisesCount)
        {
            throw new InvalidOperationException("Complete all exercises first.");
        }

        var progress = await learningRepository.GetProgress(childId, lessonId);
        if (progress is null)
        {
            progress = new ChildLessonProgress { ChildId = childId, LessonId = lessonId };
            await learningRepository.AddProgress(progress);
        }

        if (progress.Status != LessonProgressStatus.Completed)
        {
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
        }

        await unitOfWork.SaveChanges();
        return new { ChildId = childId, child.XpTotal, child.CurrentLevel, child.StreakCurrent };
    }

    private async Task EnsureSequentialAccess(Guid childId, Guid lessonId)
    {
        var lesson = await curriculumRepository.GetLesson(lessonId) ?? throw new KeyNotFoundException("Lesson not found.");
        var allLessons = await curriculumRepository.GetLessons(new LessonQueryOptions { UnitId = lesson.UnitId, IsPublished = true, Page = 1, PageSize = 200 });
        var previousLesson = allLessons.Items.Where(x => x.OrderIndex < lesson.OrderIndex).OrderByDescending(x => x.OrderIndex).FirstOrDefault();
        if (previousLesson is null) return;

        var previousProgress = await learningRepository.GetProgress(childId, previousLesson.Id);
        var done = previousProgress?.Status == LessonProgressStatus.Completed;
        if (!done) throw new InvalidOperationException("Previous lesson must be completed first.");
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
