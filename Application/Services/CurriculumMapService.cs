using LearningPlatform.Application;
using LearningPlatform.Domain;
using LearningPlatform.Infrastructure.Repositories;

namespace LearningPlatform.Application.Services;

/// <summary>B1: агрегат программы, статусов юнитов/уроков и подсказки «что дальше».</summary>
public class CurriculumMapService(
    IChildRepository childRepository,
    ICurriculumRepository curriculumRepository,
    ILearningRepository learningRepository,
    IContentLocalizationService localization,
    CurrentUnitProgressService currentUnitProgress)
{
    public async Task<CurriculumMapResponse> GetMapAsync(Guid childId, string locale)
    {
        var child = await childRepository.GetById(childId) ?? throw new KeyNotFoundException("Child not found.");
        if (child.CurrentProgram is null)
            throw new InvalidOperationException("Child.CurrentProgram must be loaded.");

        var program = await curriculumRepository.GetProgram(child.CurrentProgramId)
                      ?? throw new KeyNotFoundException("Program not found.");

        var (programTitle, programDescription) = await localization.GetLocalizedProgramAsync(program, locale);

        var unitsPage = await curriculumRepository.GetUnits(
            new UnitQueryOptions { ProgramId = child.CurrentProgramId, Page = 1, PageSize = 500 },
            restrictToPublishedCatalog: true);
        var units = unitsPage.Items.OrderBy(u => u.OrderIndex).ToList();
        await localization.ApplyUnitsAsync(units, locale);

        var unitDtos = new List<CurriculumMapUnitDto>();
        Guid? nextLessonId = null;
        Guid? nextUnitId = null;
        string nextSummary = "All published lessons in this program are completed.";

        foreach (var unit in units)
        {
            var lessonsPage = await curriculumRepository.GetLessons(
                new LessonQueryOptions { UnitId = unit.Id, ProgramId = child.CurrentProgramId, Page = 1, PageSize = 500 },
                restrictToPublishedCatalog: true);
            var lessons = lessonsPage.Items.OrderBy(l => l.OrderIndex).ToList();
            await localization.ApplyLessonsAsync(lessons, locale);

            var locked = false;
            foreach (var ou in units.Where(u => u.OrderIndex < unit.OrderIndex))
            {
                if (!await AllPublishedLessonsCompletedAsync(child.Id, ou.Id))
                {
                    locked = true;
                    break;
                }
            }

            var lessonIds = lessons.Select(l => l.Id).ToList();
            var progressMap = await learningRepository.GetLessonProgressMapAsync(child.Id, lessonIds);

            var lessonDtos = new List<CurriculumMapLessonDto>();
            foreach (var lesson in lessons)
            {
                var st = MapLessonStatus(locked, progressMap.GetValueOrDefault(lesson.Id));
                lessonDtos.Add(new CurriculumMapLessonDto(lesson.Id, lesson.Title, lesson.OrderIndex, st));
            }

            var unitStatus = MapUnitStatus(locked, lessons, progressMap);
            unitDtos.Add(new CurriculumMapUnitDto(unit.Id, unit.Title, unit.Description, unit.OrderIndex, unitStatus, lessonDtos));

            if (nextLessonId is null && !locked && lessons.Count > 0)
            {
                foreach (var lesson in lessons)
                {
                    var p = progressMap.GetValueOrDefault(lesson.Id);
                    if (p?.Status != LessonProgressStatus.Completed)
                    {
                        nextLessonId = lesson.Id;
                        nextUnitId = unit.Id;
                        nextSummary = $"Next lesson: {lesson.Title}";
                        break;
                    }
                }
            }
        }

        var (progressPercent, currentUnitId) =
            await currentUnitProgress.ComputeAsync(child.Id, child.CurrentProgramId);

        return new CurriculumMapResponse(
            child.Id,
            program.Id,
            program.DifficultyTrack,
            programTitle,
            programDescription,
            progressPercent,
            currentUnitId,
            unitDtos,
            new CurriculumMapNextDto(nextSummary, nextLessonId, nextUnitId));
    }

    private async Task<bool> AllPublishedLessonsCompletedAsync(Guid childId, Guid unitId)
    {
        var total = await curriculumRepository.CountPublishedLessonsInUnit(unitId);
        if (total == 0) return true;
        var done = await learningRepository.CountCompletedPublishedLessonsInUnitAsync(childId, unitId);
        return done >= total;
    }

    private static CurriculumMapLessonStatus MapLessonStatus(bool unitLocked, ChildLessonProgress? progress)
    {
        if (unitLocked)
            return CurriculumMapLessonStatus.Locked;
        if (progress is null)
            return CurriculumMapLessonStatus.NotStarted;
        return progress.Status switch
        {
            LessonProgressStatus.Completed => CurriculumMapLessonStatus.Completed,
            LessonProgressStatus.NotStarted => CurriculumMapLessonStatus.NotStarted,
            _ => CurriculumMapLessonStatus.InProgress
        };
    }

    private static CurriculumMapUnitStatus MapUnitStatus(bool locked, IReadOnlyList<Lesson> lessons, Dictionary<Guid, ChildLessonProgress> progressMap)
    {
        if (locked)
            return CurriculumMapUnitStatus.Locked;
        if (lessons.Count == 0)
            return CurriculumMapUnitStatus.Open;

        var allCompleted = lessons.All(l =>
            progressMap.GetValueOrDefault(l.Id)?.Status == LessonProgressStatus.Completed);
        return allCompleted ? CurriculumMapUnitStatus.Completed : CurriculumMapUnitStatus.Open;
    }
}
