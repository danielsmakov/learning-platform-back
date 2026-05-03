using Hangfire;
using LearningPlatform.Domain;
using LearningPlatform.Infrastructure.Repositories;

namespace LearningPlatform.Application.Services;

public class BadgeEvaluationJob(
    IChildRepository childRepository,
    IBadgeRepository badgeRepository,
    ILearningRepository learningRepository,
    INotificationRepository notificationRepository,
    IUnitOfWork unitOfWork,
    IParentNotificationPublisher notificationPublisher)
{
    /// <summary>F1 / общая проверка: первый урок, XP, стрик и т.д.</summary>
    [AutomaticRetry(Attempts = 2)]
    public async Task Evaluate(Guid childId)
    {
        var child = await childRepository.GetById(childId);
        if (child is null) return;

        await EnsureBadgeSeedAsync();
        var badges = await badgeRepository.GetAll();
        var earnedBadgeIds = await badgeRepository.GetEarnedBadgeIds(childId);
        var completedLessons = await learningRepository.CountCompletedLessons(childId);
        var unlockNotifications = new List<Notification>();

        foreach (var badge in badges.Where(x => !earnedBadgeIds.Contains(x.Id)))
        {
            if (IsUnitScopedBadgeKey(badge.Key))
                continue;

            var unlock = badge.Key switch
            {
                "first_lesson" => completedLessons >= 1,
                "xp_100" => child.XpTotal >= 100,
                "streak_7" => child.StreakCurrent >= 7,
                _ => false
            };

            if (!unlock) continue;
            await badgeRepository.AddChildBadge(new ChildBadge { ChildId = childId, BadgeId = badge.Id });
            var notification = CreateBadgeNotification(child, badge);
            await notificationRepository.Add(notification);
            unlockNotifications.Add(notification);
        }

        await unitOfWork.SaveChanges();
        foreach (var n in unlockNotifications)
            await notificationPublisher.PublishSavedAsync(n);
    }

    /// <summary>E5: условия по ошибкам за завершённый юнит (до удаления записи адаптивной логикой).</summary>
    [AutomaticRetry(Attempts = 2)]
    public async Task EvaluateAfterUnitCompletionAsync(Guid childId, Guid unitId)
    {
        var cup = await learningRepository.GetChildUnitProgressAsync(childId, unitId);
        if (cup is null || cup.Status != UnitProgressStatus.Completed)
            return;

        var child = await childRepository.GetById(childId);
        if (child is null) return;

        await EnsureBadgeSeedAsync();
        var badges = await badgeRepository.GetAll();
        var earnedBadgeIds = await badgeRepository.GetEarnedBadgeIds(childId);
        var errors = cup.ErrorCount;
        var unlockNotifications = new List<Notification>();

        foreach (var badge in badges.Where(x => !earnedBadgeIds.Contains(x.Id)))
        {
            if (!IsUnitScopedBadgeKey(badge.Key))
                continue;

            var unlock = UnlocksUnitBadge(badge.Key, errors);
            if (!unlock) continue;

            await badgeRepository.AddChildBadge(new ChildBadge { ChildId = childId, BadgeId = badge.Id });
            var notification = CreateBadgeNotification(child, badge);
            await notificationRepository.Add(notification);
            unlockNotifications.Add(notification);
        }

        await unitOfWork.SaveChanges();
        foreach (var n in unlockNotifications)
            await notificationPublisher.PublishSavedAsync(n);
    }

    /// <summary>F1: периодическая переоценка для всех детей.</summary>
    public async Task EvaluateAllChildrenAsync()
    {
        foreach (var childId in await childRepository.GetAllChildIdsAsync())
            await Evaluate(childId);
    }

    private static bool IsUnitScopedBadgeKey(string key) =>
        key is "unit_flawless" or "unit_steady" or "unit_tenacious";

    private static bool UnlocksUnitBadge(string key, int errors) => key switch
    {
        "unit_flawless" => errors == 0,
        "unit_steady" => errors is >= 1 and <= 3,
        "unit_tenacious" => errors >= 10,
        _ => false
    };

    private static Notification CreateBadgeNotification(Child child, Badge badge) => new()
    {
        ParentId = child.ParentId,
        ChildId = child.Id,
        Type = NotificationType.BadgeEarned,
        Title = "Badge earned",
        Body = $"{child.DisplayName} earned badge: {badge.Name}."
    };

    private async Task EnsureBadgeSeedAsync()
    {
        foreach (var def in BadgeSeedDefinitions.All)
        {
            if (await badgeRepository.GetByKey(def.Key) is not null)
                continue;
            await badgeRepository.Add(new Badge
            {
                Key = def.Key,
                Name = def.Name,
                ConditionType = def.ConditionType,
                ConditionValue = def.ConditionValue
            });
        }

        await unitOfWork.SaveChanges();
    }
}

internal static class BadgeSeedDefinitions
{
    public sealed record Def(string Key, string Name, string ConditionType, int ConditionValue);

    /// <summary>F1: базовые + три юнит-бейджа (метрики ошибок за прохождение юнита).</summary>
    public static readonly Def[] All =
    [
        new("first_lesson", "First Lesson", "lessons_completed", 1),
        new("xp_100", "100 XP", "xp_total", 100),
        new("streak_7", "7-Day Streak", "streak_current", 7),
        new("unit_flawless", "Flawless Unit", "unit_errors", 0),
        new("unit_steady", "Steady Progress", "unit_errors_max_band", 3),
        new("unit_tenacious", "Tenacious Learner", "unit_errors_min", 10)
    ];
}
