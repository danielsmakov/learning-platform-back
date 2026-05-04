using LearningPlatform.Application;
using LearningPlatform.Application.Services;
using LearningPlatform.Domain;
using LearningPlatform.Infrastructure.Repositories;
using Moq;
using Xunit;

namespace LearningPlatform.Tests;

public class CurrentUnitProgressServiceTests
{
    private readonly Guid _programId = Guid.Parse("a1000000-0000-4000-8000-000000000001");
    private readonly Guid _childId = Guid.Parse("b1000000-0000-4000-8000-000000000001");

    [Fact]
    public async Task ComputeAsync_no_units_returns_zero_and_null_current()
    {
        var curriculum = new Mock<ICurriculumRepository>();
        var learning = new Mock<ILearningRepository>();

        curriculum
            .Setup(c => c.GetUnits(
                It.Is<UnitQueryOptions>(q => q.ProgramId == _programId && q.Page == 1 && q.PageSize == 500),
                true))
            .ReturnsAsync(new PagedResponse<Unit>
            {
                Items = [],
                Total = 0,
                Page = 1,
                PageSize = 500,
                TotalPages = 0
            });

        var sut = new CurrentUnitProgressService(curriculum.Object, learning.Object);
        var (percent, currentUnitId) = await sut.ComputeAsync(_childId, _programId);

        Assert.Equal(0, percent);
        Assert.Null(currentUnitId);
    }

    [Fact]
    public async Task ComputeAsync_single_unit_half_completed_returns_50_percent()
    {
        var unitId = Guid.Parse("c1000000-0000-4000-8000-000000000001");
        var curriculum = new Mock<ICurriculumRepository>();
        var learning = new Mock<ILearningRepository>();

        curriculum
            .Setup(c => c.GetUnits(
                It.Is<UnitQueryOptions>(q => q.ProgramId == _programId),
                true))
            .ReturnsAsync(new PagedResponse<Unit>
            {
                Items =
                [
                    new Unit
                    {
                        Id = unitId,
                        ProgramId = _programId,
                        Title = "U1",
                        Description = "",
                        OrderIndex = 0,
                        IsPublished = true
                    }
                ],
                Total = 1,
                Page = 1,
                PageSize = 500,
                TotalPages = 1
            });

        curriculum.Setup(c => c.CountPublishedLessonsInUnit(unitId)).ReturnsAsync(4);
        learning.Setup(l => l.CountCompletedPublishedLessonsInUnitAsync(_childId, unitId)).ReturnsAsync(2);

        var sut = new CurrentUnitProgressService(curriculum.Object, learning.Object);
        var (percent, currentUnitId) = await sut.ComputeAsync(_childId, _programId);

        Assert.Equal(50, percent);
        Assert.Equal(unitId, currentUnitId);
    }

    [Fact]
    public async Task ComputeAsync_when_all_lessons_in_program_completed_returns_100_and_last_unit()
    {
        var u1 = Guid.Parse("d1000000-0000-4000-8000-000000000001");
        var u2 = Guid.Parse("d1000000-0000-4000-8000-000000000002");

        var curriculum = new Mock<ICurriculumRepository>();
        var learning = new Mock<ILearningRepository>();

        curriculum
            .Setup(c => c.GetUnits(It.IsAny<UnitQueryOptions>(), true))
            .ReturnsAsync(new PagedResponse<Unit>
            {
                Items =
                [
                    new Unit
                    {
                        Id = u1,
                        ProgramId = _programId,
                        Title = "A",
                        Description = "",
                        OrderIndex = 0,
                        IsPublished = true
                    },
                    new Unit
                    {
                        Id = u2,
                        ProgramId = _programId,
                        Title = "B",
                        Description = "",
                        OrderIndex = 1,
                        IsPublished = true
                    }
                ],
                Total = 2,
                Page = 1,
                PageSize = 500,
                TotalPages = 1
            });

        curriculum.Setup(c => c.CountPublishedLessonsInUnit(u1)).ReturnsAsync(1);
        learning.Setup(l => l.CountCompletedPublishedLessonsInUnitAsync(_childId, u1)).ReturnsAsync(1);

        curriculum.Setup(c => c.CountPublishedLessonsInUnit(u2)).ReturnsAsync(2);
        learning.Setup(l => l.CountCompletedPublishedLessonsInUnitAsync(_childId, u2)).ReturnsAsync(2);

        var sut = new CurrentUnitProgressService(curriculum.Object, learning.Object);
        var (percent, currentUnitId) = await sut.ComputeAsync(_childId, _programId);

        Assert.Equal(100, percent);
        Assert.Equal(u2, currentUnitId);
    }
}
