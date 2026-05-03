using LearningPlatform.Application;
using LearningPlatform.Domain;
using LearningPlatform.Infrastructure.Repositories;

namespace LearningPlatform.Application.Services;

public class CurriculumService(
    ICurriculumRepository curriculumRepository,
    IActivityLogRepository activityLogRepository,
    IUnitOfWork unitOfWork,
    IContentLocalizationService localization)
{
    public async Task<PagedResponse<LearningProgram>> GetPrograms(QueryOptions query, bool includeUnpublished, string? acceptLanguage = null)
    {
        var page = await curriculumRepository.GetPrograms(query, includeUnpublished);
        if (!string.IsNullOrWhiteSpace(acceptLanguage))
            await localization.ApplyProgramsAsync(page.Items.ToList(), LocalePreference.Parse(acceptLanguage));
        return page;
    }

    public async Task<LearningProgram> CreateProgram(CreateLearningProgramRequest request, Guid adminId)
    {
        if (await curriculumRepository.GetProgramByTrack(request.DifficultyTrack) is not null)
            throw new InvalidOperationException("A program with this difficulty track already exists.");

        var program = new LearningProgram
        {
            DifficultyTrack = request.DifficultyTrack,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            IsPublished = request.IsPublished,
            IsDefault = false
        };
        await curriculumRepository.AddProgram(program);
        await Log(adminId, "create", "program", program.Id.ToString());
        await unitOfWork.SaveChanges();
        return program;
    }

    public async Task<LearningProgram> UpdateProgram(Guid id, UpdateLearningProgramRequest request, Guid adminId)
    {
        var program = await curriculumRepository.GetProgram(id) ?? throw new KeyNotFoundException("Program not found.");
        program.Title = request.Title.Trim();
        program.Description = request.Description.Trim();
        program.IsPublished = request.IsPublished;
        await Log(adminId, "update", "program", id.ToString());
        await unitOfWork.SaveChanges();
        return program;
    }

    public async Task DeleteProgram(Guid id, Guid adminId)
    {
        var program = await curriculumRepository.GetProgram(id) ?? throw new KeyNotFoundException("Program not found.");
        if (await curriculumRepository.CountUnitsForProgram(id) > 0)
            throw new InvalidOperationException("Cannot delete a program that still has units.");
        if (await curriculumRepository.CountChildrenUsingProgram(id) > 0)
            throw new InvalidOperationException("Cannot delete a program assigned to one or more children.");
        await curriculumRepository.DeleteProgram(program);
        await Log(adminId, "delete", "program", id.ToString());
        await unitOfWork.SaveChanges();
    }

    public async Task<PagedResponse<Unit>> GetUnits(UnitQueryOptions query, bool includeUnpublished = false, string? acceptLanguage = null)
    {
        var page = await curriculumRepository.GetUnits(query, restrictToPublishedCatalog: !includeUnpublished);
        if (!string.IsNullOrWhiteSpace(acceptLanguage))
            await localization.ApplyUnitsAsync(page.Items.ToList(), LocalePreference.Parse(acceptLanguage));
        return page;
    }

    /// <summary>A2: одна карточка юнита в контексте программы каталога; G2 — локализация через Accept-Language.</summary>
    public async Task<Unit> GetUnitCatalogCard(Guid unitId, Guid programId, bool includeUnpublished, string? acceptLanguage)
    {
        var unit = await curriculumRepository.GetUnit(unitId) ?? throw new KeyNotFoundException("Unit not found.");
        if (unit.ProgramId != programId)
            throw new InvalidOperationException("Unit is not in the resolved program.");
        if (!includeUnpublished && !unit.IsPublished)
            throw new KeyNotFoundException("Unit not found.");
        if (!string.IsNullOrWhiteSpace(acceptLanguage))
            await localization.ApplyUnitsAsync([unit], LocalePreference.Parse(acceptLanguage));
        return unit;
    }

    public async Task<Unit> CreateUnit(CreateUnitRequest request, Guid adminId, Guid programContextId)
    {
        if (request.ProgramId != programContextId)
            throw new InvalidOperationException("Body ProgramId must match query programId.");
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

    public async Task<Unit> UpdateUnit(Guid id, UpdateUnitRequest request, Guid adminId, Guid programContextId)
    {
        var unit = await curriculumRepository.GetUnit(id) ?? throw new KeyNotFoundException("Unit not found.");
        var resultingProgramId = request.ProgramId ?? unit.ProgramId;
        if (resultingProgramId != programContextId)
            throw new InvalidOperationException("Query programId must match the unit's program (or the target program when moving a unit).");
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

    public async Task DeleteUnit(Guid id, Guid adminId, Guid programContextId)
    {
        var unit = await curriculumRepository.GetUnit(id) ?? throw new KeyNotFoundException("Unit not found.");
        if (unit.ProgramId != programContextId)
            throw new InvalidOperationException("Query programId does not match this unit's program.");
        await curriculumRepository.DeleteUnit(unit);
        await Log(adminId, "delete", "unit", id.ToString());
        await unitOfWork.SaveChanges();
    }

    public async Task<PagedResponse<Lesson>> GetLessons(LessonQueryOptions query, bool includeUnpublished = false, string? acceptLanguage = null)
    {
        var page = await curriculumRepository.GetLessons(query, restrictToPublishedCatalog: !includeUnpublished);
        if (!string.IsNullOrWhiteSpace(acceptLanguage))
            await localization.ApplyLessonsAsync(page.Items.ToList(), LocalePreference.Parse(acceptLanguage));
        return page;
    }

    /// <summary>A2: одна карточка урока; G2 — локализация заголовка через Accept-Language.</summary>
    public async Task<Lesson> GetLessonCatalogCard(Guid lessonId, Guid programId, bool includeUnpublished, string? acceptLanguage)
    {
        await EnsureLessonBelongsToProgram(lessonId, programId);
        await EnsureLessonPublishedForCatalog(lessonId, includeUnpublished);
        var lesson = await curriculumRepository.GetLesson(lessonId) ?? throw new KeyNotFoundException("Lesson not found.");
        if (!string.IsNullOrWhiteSpace(acceptLanguage))
            await localization.ApplyLessonsAsync([lesson], LocalePreference.Parse(acceptLanguage));
        return lesson;
    }

    public async Task<Lesson> CreateLesson(CreateLessonRequest request, Guid adminId, Guid programContextId)
    {
        var unit = await curriculumRepository.GetUnit(request.UnitId) ?? throw new KeyNotFoundException("Unit not found.");
        if (unit.ProgramId != programContextId)
            throw new InvalidOperationException("Unit is not in the program specified by programId.");
        var lesson = new Lesson
        {
            UnitId = request.UnitId,
            Title = request.Title,
            Description = request.Description,
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

    public async Task<Lesson> UpdateLesson(Guid id, UpdateLessonRequest request, Guid adminId, Guid programContextId)
    {
        var lesson = await curriculumRepository.GetLesson(id) ?? throw new KeyNotFoundException("Lesson not found.");
        if (lesson.Unit is null || lesson.Unit.ProgramId != programContextId)
            throw new InvalidOperationException("Query programId does not match this lesson's program.");
        lesson.Title = request.Title;
        lesson.Description = request.Description;
        lesson.OrderIndex = request.OrderIndex;
        lesson.LessonType = request.LessonType;
        lesson.Difficulty = request.Difficulty;
        lesson.XpReward = request.XpReward;
        lesson.IsPublished = request.IsPublished;
        await Log(adminId, "update", "lesson", id.ToString());
        await unitOfWork.SaveChanges();
        return lesson;
    }

    public async Task DeleteLesson(Guid id, Guid adminId, Guid programContextId)
    {
        var lesson = await curriculumRepository.GetLesson(id) ?? throw new KeyNotFoundException("Lesson not found.");
        if (lesson.Unit is null || lesson.Unit.ProgramId != programContextId)
            throw new InvalidOperationException("Query programId does not match this lesson's program.");
        await curriculumRepository.DeleteLesson(lesson);
        await Log(adminId, "delete", "lesson", id.ToString());
        await unitOfWork.SaveChanges();
    }

    public async Task<PagedResponse<Exercise>> GetExercises(Guid lessonId, QueryOptions query, bool includeUnpublished = false, string? acceptLanguage = null)
    {
        var page = await curriculumRepository.GetExercises(lessonId, query, restrictToPublishedCatalog: !includeUnpublished);
        if (!string.IsNullOrWhiteSpace(acceptLanguage))
            await localization.ApplyExercisesAsync(page.Items.ToList(), LocalePreference.Parse(acceptLanguage));
        return page;
    }

    public async Task EnsureLessonBelongsToProgram(Guid lessonId, Guid programId)
    {
        var lesson = await curriculumRepository.GetLesson(lessonId) ?? throw new KeyNotFoundException("Lesson not found.");
        if (lesson.Unit is null || lesson.Unit.ProgramId != programId)
            throw new InvalidOperationException("Lesson is not in the resolved program.");
    }

    /// <summary>A1: черновики недоступны в каталоге без явного admin includeUnpublished.</summary>
    public async Task EnsureLessonPublishedForCatalog(Guid lessonId, bool includeUnpublished)
    {
        if (includeUnpublished) return;
        var lesson = await curriculumRepository.GetLesson(lessonId) ?? throw new KeyNotFoundException("Lesson not found.");
        if (!lesson.IsPublished || lesson.Unit is null || !lesson.Unit.IsPublished)
            throw new KeyNotFoundException("Lesson not found.");
    }

    public async Task<Exercise> CreateExercise(Guid lessonId, CreateExerciseRequest request, Guid adminId, Guid programContextId)
    {
        var lesson = await curriculumRepository.GetLesson(lessonId) ?? throw new KeyNotFoundException("Lesson not found.");
        if (lesson.Unit is null || lesson.Unit.ProgramId != programContextId)
            throw new InvalidOperationException("Query programId does not match this lesson's program.");
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

    public async Task<Exercise> UpdateExercise(Guid id, UpdateExerciseRequest request, Guid adminId, Guid programContextId)
    {
        var exercise = await curriculumRepository.GetExercise(id) ?? throw new KeyNotFoundException("Exercise not found.");
        if (exercise.Lesson?.Unit is null || exercise.Lesson.Unit.ProgramId != programContextId)
            throw new InvalidOperationException("Query programId does not match this exercise's program.");
        exercise.ExerciseType = request.ExerciseType;
        exercise.OrderIndex = request.OrderIndex;
        exercise.Content = request.Content;
        await Log(adminId, "update", "exercise", id.ToString());
        await unitOfWork.SaveChanges();
        return exercise;
    }

    public async Task DeleteExercise(Guid id, Guid adminId, Guid programContextId)
    {
        var exercise = await curriculumRepository.GetExercise(id) ?? throw new KeyNotFoundException("Exercise not found.");
        if (exercise.Lesson?.Unit is null || exercise.Lesson.Unit.ProgramId != programContextId)
            throw new InvalidOperationException("Query programId does not match this exercise's program.");
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
