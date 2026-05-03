using LearningPlatform.Application;
using LearningPlatform.Domain;
using LearningPlatform.Infrastructure.Repositories;

namespace LearningPlatform.Application.Services;

public class NotificationService(INotificationRepository notificationRepository, IChildRepository childRepository, IUnitOfWork unitOfWork)
{
    public Task<PagedResponse<Notification>> GetNotifications(Guid parentId, QueryOptions query) => notificationRepository.GetByParent(parentId, query);

    public async Task MarkRead(Guid parentId, IReadOnlyCollection<Guid> notificationIds)
    {
        var notifications = await notificationRepository.GetByParentAndIds(parentId, notificationIds);
        foreach (var notification in notifications)
            notification.IsRead = true;
        await unitOfWork.SaveChanges();
    }

    public async Task CreateDailyStreakReminders()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var children = await childRepository.GetNotActiveToday(today);
        foreach (var child in children.Where(c => c.LastActivityDate != today))
        {
            await notificationRepository.Add(new Notification
            {
                ParentId = child.ParentId,
                ChildId = child.Id,
                Type = NotificationType.StreakReminder,
                Title = "Streak at risk",
                Body = $"{child.DisplayName} has not practiced today yet."
            });
        }

        await unitOfWork.SaveChanges();
    }

    public async Task CreateWeeklySummaries()
    {
        var parentIds = await childRepository.DistinctParentIds();
        foreach (var parentId in parentIds)
        {
            await notificationRepository.Add(new Notification
            {
                ParentId = parentId,
                Type = NotificationType.WeeklySummary,
                Title = "Weekly progress summary",
                Body = "Your weekly progress report is ready."
            });
        }

        await unitOfWork.SaveChanges();
    }
}
