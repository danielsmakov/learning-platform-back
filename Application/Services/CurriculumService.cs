using LearningPlatform.Application;
using LearningPlatform.Domain;
using LearningPlatform.Infrastructure.Repositories;

namespace LearningPlatform.Application.Services;

public class CurriculumService(ICurriculumRepository curriculumRepository, IActivityLogRepository activityLogRepository, IUnitOfWork unitOfWork)
{
    public Task<PagedResponse<Unit>> GetUnits(UnitQueryOptions query) => curriculumRepository.GetUnits(query);

    public async Task<Unit> CreateUnit(CreateUnitRequest request, Guid adminId)
    {
        _ = await curriculumRepository.GetProgram(request.ProgramId) ?? throw new KeyNotFoundException("Program not found.");
        var unit = new Unit
        {
            ProgramId = request.ProgramId,
            Title = request.Title,
            Description = request.Description,
            OrderIndex = request.OrderIndex,
            IsPublished = request.IsPublished
        };
        await curriculumRepository.AddUnit(unit);
        await Log(adminId, "create", "unit", unit.Id.ToString());
        await unitOfWork.SaveChanges();
        return unit;
    }

    public async Task<Unit> UpdateUnit(Guid id, UpdateUnitRequest request, Guid adminId)
    {
        var unit = await curriculumRepository.GetUnit(id) ?? throw new KeyNotFoundException("Unit not found.");
        unit.Title = request.Title;
        unit.Description = request.Description;
        unit.OrderIndex = request.OrderIndex;
        unit.IsPublished = request.IsPublished;
        if (request.ProgramId.HasValue)
        {
            _ = await curriculumRepository.GetProgram(request.ProgramId.Value) ?? throw new KeyNotFoundException("Program not found.");
            unit.ProgramId = request.ProgramId.Value;
        }
        await Log(adminId, "update", "unit", id.ToString());
        await unitOfWork.SaveChanges();
        return unit;
    }

    public async Task DeleteUnit(Guid id, Guid adminId)
    {
        var unit = await curriculumRepository.GetUnit(id) ?? throw new KeyNotFoundException("Unit not found.");
        await curriculumRepository.DeleteUnit(unit);
        await Log(adminId, "delete", "unit", id.ToString());
        await unitOfWork.SaveChanges();
    }

    public Task<PagedResponse<Lesson>> GetLessons(LessonQueryOptions query) => curriculumRepository.GetLessons(query);

    public async Task<Lesson> CreateLesson(CreateLessonRequest request, Guid adminId)
    {
        var lesson = new Lesson
        {
            UnitId = request.UnitId,
            Title = request.Title,
            OrderIndex = request.OrderIndex,
            LessonType = request.LessonType,
            Difficulty = request.Difficulty,
            XpReward = request.XpReward,
            IsPublished = request.IsPublished
        };
        await curriculumRepository.AddLesson(lesson);
        await Log(adminId, "create", "lesson", lesson.Id.ToString());
        await unitOfWork.SaveChanges();
        return lesson;
    }

    public async Task<Lesson> UpdateLesson(Guid id, UpdateLessonRequest request, Guid adminId)
    {
        var lesson = await curriculumRepository.GetLesson(id) ?? throw new KeyNotFoundException("Lesson not found.");
        lesson.Title = request.Title;
        lesson.OrderIndex = request.OrderIndex;
        lesson.LessonType = request.LessonType;
        lesson.Difficulty = request.Difficulty;
        lesson.XpReward = request.XpReward;
        lesson.IsPublished = request.IsPublished;
        await Log(adminId, "update", "lesson", id.ToString());
        await unitOfWork.SaveChanges();
        return lesson;
    }

    public async Task DeleteLesson(Guid id, Guid adminId)
    {
        var lesson = await curriculumRepository.GetLesson(id) ?? throw new KeyNotFoundException("Lesson not found.");
        await curriculumRepository.DeleteLesson(lesson);
        await Log(adminId, "delete", "lesson", id.ToString());
        await unitOfWork.SaveChanges();
    }

    public Task<PagedResponse<Exercise>> GetExercises(Guid lessonId, QueryOptions query) => curriculumRepository.GetExercises(lessonId, query);

    public async Task<Exercise> CreateExercise(Guid lessonId, CreateExerciseRequest request, Guid adminId)
    {
        var exercise = new Exercise
        {
            LessonId = lessonId,
            ExerciseType = request.ExerciseType,
            OrderIndex = request.OrderIndex,
            Content = request.Content
        };
        await curriculumRepository.AddExercise(exercise);
        await Log(adminId, "create", "exercise", exercise.Id.ToString());
        await unitOfWork.SaveChanges();
        return exercise;
    }

    public async Task<Exercise> UpdateExercise(Guid id, UpdateExerciseRequest request, Guid adminId)
    {
        var exercise = await curriculumRepository.GetExercise(id) ?? throw new KeyNotFoundException("Exercise not found.");
        exercise.ExerciseType = request.ExerciseType;
        exercise.OrderIndex = request.OrderIndex;
        exercise.Content = request.Content;
        await Log(adminId, "update", "exercise", id.ToString());
        await unitOfWork.SaveChanges();
        return exercise;
    }

    public async Task DeleteExercise(Guid id, Guid adminId)
    {
        var exercise = await curriculumRepository.GetExercise(id) ?? throw new KeyNotFoundException("Exercise not found.");
        await curriculumRepository.DeleteExercise(exercise);
        await Log(adminId, "delete", "exercise", id.ToString());
        await unitOfWork.SaveChanges();
    }

    private Task Log(Guid adminId, string action, string resourceType, string resourceId)
    {
        return activityLogRepository.Add(new ActivityLog
        {
            AdminId = adminId,
            Action = action,
            ResourceType = resourceType,
            ResourceId = resourceId
        });
    }
}
