using LearningPlatform.Application;
using LearningPlatform.Infrastructure.Repositories;

namespace LearningPlatform.Application.Services;

/// <summary>G1: единый расчёт «текущего юнита» и процента завершённых опубликованных уроков (см. <see cref="ChildResponse"/>).</summary>
public class CurrentUnitProgressService(ICurriculumRepository curriculumRepository, ILearningRepository learningRepository)
{
    public async Task<(int Percent, Guid? CurrentUnitId)> ComputeAsync(Guid childId, Guid currentProgramId)
    {
        var unitsPage = await curriculumRepository.GetUnits(
            new UnitQueryOptions { ProgramId = currentProgramId, Page = 1, PageSize = 500 },
            restrictToPublishedCatalog: true);
        var units = unitsPage.Items.OrderBy(u => u.OrderIndex).ToList();

        Guid? lastUnitWithLessons = null;
        foreach (var unit in units)
        {
            var total = await curriculumRepository.CountPublishedLessonsInUnit(unit.Id);
            if (total == 0) continue;
            lastUnitWithLessons = unit.Id;
            var completed = await learningRepository.CountCompletedPublishedLessonsInUnitAsync(childId, unit.Id);
            if (completed < total)
            {
                var percent = (int)Math.Round(100.0 * completed / total);
                return (percent, unit.Id);
            }
        }

        if (lastUnitWithLessons.HasValue)
            return (100, lastUnitWithLessons);

        return (0, null);
    }
}
