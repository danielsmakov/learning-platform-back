using LearningPlatform.Application.Services;
using LearningPlatform.Domain;
using LearningPlatform.Infrastructure.Repositories;
using Moq;
using Xunit;

namespace LearningPlatform.Tests;

public class BadgeEvaluationJobTests
{
    [Fact]
    public async Task Evaluate_awards_first_lesson_when_completed_count_reaches_one()
    {
        var childId = Guid.NewGuid();
        var parentId = Guid.NewGuid();
        var firstLessonBadgeId = Guid.Parse("f1000000-0000-4000-8000-000000000001");

        var child = new Child
        {
            Id = childId,
            ParentId = parentId,
            Name = "C",
            Age = 7,
            AvatarUrl = "x",
            DisplayName = "Kid",
            PinHash = "x",
            CurrentProgramId = Guid.NewGuid(),
            XpTotal = 0,
            StreakCurrent = 0
        };

        var childRepo = new Mock<IChildRepository>();
        childRepo.Setup(c => c.GetById(childId)).ReturnsAsync(child);

        var badgeRepo = new Mock<IBadgeRepository>();
        // Сиды уже есть — не создаём новые строки Badge в EnsureBadgeSeedAsync
        badgeRepo.Setup(b => b.GetByKey(It.IsAny<string>())).ReturnsAsync((Badge?)null);
        badgeRepo.Setup(b => b.Add(It.IsAny<Badge>())).Returns(Task.CompletedTask);

        var firstLessonBadge = new Badge
        {
            Id = firstLessonBadgeId,
            Key = "first_lesson",
            Name = "First",
            ConditionType = "lessons_completed",
            ConditionValue = 1
        };
        var xpBadge = new Badge
        {
            Id = Guid.Parse("f1000000-0000-4000-8000-000000000002"),
            Key = "xp_100",
            Name = "100 XP",
            ConditionType = "xp_total",
            ConditionValue = 100
        };

        badgeRepo.Setup(b => b.GetAll()).ReturnsAsync([firstLessonBadge, xpBadge]);
        badgeRepo.Setup(b => b.GetEarnedBadgeIds(childId)).ReturnsAsync([]);
        badgeRepo.Setup(b => b.AddChildBadge(It.IsAny<ChildBadge>())).Returns(Task.CompletedTask);

        var learningRepo = new Mock<ILearningRepository>();
        learningRepo.Setup(l => l.CountCompletedLessons(childId)).ReturnsAsync(1);

        var notifRepo = new Mock<INotificationRepository>();
        notifRepo.Setup(n => n.Add(It.IsAny<Notification>())).Returns(Task.CompletedTask);

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(x => x.SaveChanges()).Returns(Task.CompletedTask);

        var publisher = new Mock<IParentNotificationPublisher>();
        publisher.Setup(p => p.PublishSavedAsync(It.IsAny<Notification>())).Returns(Task.CompletedTask);

        var job = new BadgeEvaluationJob(
            childRepo.Object,
            badgeRepo.Object,
            learningRepo.Object,
            notifRepo.Object,
            uow.Object,
            publisher.Object);

        await job.Evaluate(childId);

        badgeRepo.Verify(b => b.AddChildBadge(It.Is<ChildBadge>(cb => cb.BadgeId == firstLessonBadgeId)), Times.Once);
        uow.Verify(x => x.SaveChanges(), Times.AtLeastOnce);
        publisher.Verify(p => p.PublishSavedAsync(It.IsAny<Notification>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task EvaluateAfterUnitCompletionAsync_awards_unit_flawless_when_errors_zero()
    {
        var childId = Guid.NewGuid();
        var unitId = Guid.NewGuid();
        var flawlessId = Guid.Parse("e1000000-0000-4000-8000-000000000001");

        var childRepo = new Mock<IChildRepository>();
        childRepo.Setup(c => c.GetById(childId)).ReturnsAsync(new Child
        {
            Id = childId,
            ParentId = Guid.NewGuid(),
            Name = "C",
            Age = 7,
            AvatarUrl = "x",
            DisplayName = "Kid",
            PinHash = "x",
            CurrentProgramId = Guid.NewGuid(),
            XpTotal = 0,
            StreakCurrent = 0
        });

        var learningRepo = new Mock<ILearningRepository>();
        learningRepo.Setup(l => l.GetChildUnitProgressAsync(childId, unitId)).ReturnsAsync(new ChildUnitProgress
        {
            Id = Guid.NewGuid(),
            ChildId = childId,
            UnitId = unitId,
            Status = UnitProgressStatus.Completed,
            ErrorCount = 0
        });

        var flawless = new Badge
        {
            Id = flawlessId,
            Key = "unit_flawless",
            Name = "Flawless",
            ConditionType = "unit_errors",
            ConditionValue = 0
        };

        var badgeRepo = new Mock<IBadgeRepository>();
        badgeRepo.Setup(b => b.GetByKey(It.IsAny<string>())).ReturnsAsync((Badge?)null);
        badgeRepo.Setup(b => b.Add(It.IsAny<Badge>())).Returns(Task.CompletedTask);
        badgeRepo.Setup(b => b.GetAll()).ReturnsAsync([flawless]);
        badgeRepo.Setup(b => b.GetEarnedBadgeIds(childId)).ReturnsAsync([]);
        badgeRepo.Setup(b => b.AddChildBadge(It.IsAny<ChildBadge>())).Returns(Task.CompletedTask);

        var notifRepo = new Mock<INotificationRepository>();
        notifRepo.Setup(n => n.Add(It.IsAny<Notification>())).Returns(Task.CompletedTask);

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(x => x.SaveChanges()).Returns(Task.CompletedTask);

        var publisher = new Mock<IParentNotificationPublisher>();
        publisher.Setup(p => p.PublishSavedAsync(It.IsAny<Notification>())).Returns(Task.CompletedTask);

        var job = new BadgeEvaluationJob(
            childRepo.Object,
            badgeRepo.Object,
            learningRepo.Object,
            notifRepo.Object,
            uow.Object,
            publisher.Object);

        await job.EvaluateAfterUnitCompletionAsync(childId, unitId);

        badgeRepo.Verify(b => b.AddChildBadge(It.Is<ChildBadge>(cb => cb.BadgeId == flawlessId)), Times.Once);
    }
}
