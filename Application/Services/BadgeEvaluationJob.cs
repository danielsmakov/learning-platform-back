using LearningPlatform.Domain;
using LearningPlatform.Infrastructure.Repositories;

namespace LearningPlatform.Application.Services;

public class BadgeEvaluationJob(IChildRepository childRepository, IBadgeRepository badgeRepository, ILearningRepository learningRepository, INotificationRepository notificationRepository, IUnitOfWork unitOfWork)
{
    public async Task Evaluate(Guid childId)
    {
        var child = await childRepository.GetById(childId);
        if (child is null) return;

        await EnsureDefaultBadges();
        var badges = await badgeRepository.GetAll();
        var earnedBadgeIds = await badgeRepository.GetEarnedBadgeIds(childId);

        var completedLessons = await learningRepository.CountCompletedLessons(childId);

        foreach (var badge in badges.Where(x => !earnedBadgeIds.Contains(x.Id)))
        {
            var unlock = badge.Key switch
            {
                "first_lesson" => completedLessons >= 1,
                "xp_100" => child.XpTotal >= 100,
                "streak_7" => child.StreakCurrent >= 7,
                _ => false
            };

            if (!unlock) continue;
            await badgeRepository.AddChildBadge(new ChildBadge { ChildId = childId, BadgeId = badge.Id });
            await notificationRepository.Add(new Notification
            {
                ParentId = child.ParentId,
                ChildId = child.Id,
                Type = NotificationType.Milestone,
                Title = "Badge earned",
                Body = $"{child.DisplayName} earned badge: {badge.Name}."
            });
        }

        await unitOfWork.SaveChanges();
    }

    private async Task EnsureDefaultBadges()
    {
        if (await badgeRepository.Any()) return;
        await badgeRepository.AddRange(
            new Badge { Key = "first_lesson", Name = "First Lesson", ConditionType = "lessons_completed", ConditionValue = 1 },
            new Badge { Key = "xp_100", Name = "100 XP", ConditionType = "xp_total", ConditionValue = 100 },
            new Badge { Key = "streak_7", Name = "7-Day Streak", ConditionType = "streak_current", ConditionValue = 7 }
        );
        await unitOfWork.SaveChanges();
    }
}
