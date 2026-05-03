using LearningPlatform.Application;
using LearningPlatform.Domain;
using LearningPlatform.Infrastructure.Repositories;

namespace LearningPlatform.Application.Services;

public class NotificationService(
    INotificationRepository notificationRepository,
    IChildRepository childRepository,
    IUnitOfWork unitOfWork,
    IParentNotificationPublisher notificationPublisher)
{
    public Task<PagedResponse<Notification>> GetNotifications(Guid parentId, QueryOptions query) => notificationRepository.GetByParent(parentId, query);

    public async Task MarkRead(Guid parentId, IReadOnlyCollection<Guid> notificationIds)
    {
        var notifications = await notificationRepository.GetByParentAndIds(parentId, notificationIds);
        foreach (var notification in notifications)
            notification.IsRead = true;
        await unitOfWork.SaveChanges();
    }

    /// <summary>G4: ежедневное напоминание; C1 — сначала INSERT, затем SignalR.</summary>
    public async Task CreateDailyStreakReminders()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var children = await childRepository.GetNotActiveToday(today);
        var created = new List<Notification>();
        foreach (var child in children.Where(c => c.LastActivityDate != today))
        {
            var n = new Notification
            {
                ParentId = child.ParentId,
                ChildId = child.Id,
                Type = NotificationType.StreakReminder,
                Title = "Streak at risk",
                Body = $"{child.DisplayName} has not practiced today yet."
            };
            await notificationRepository.Add(n);
            created.Add(n);
        }

        await unitOfWork.SaveChanges();
        foreach (var n in created)
            await notificationPublisher.PublishSavedAsync(n);
    }

    /// <summary>P3 / C3 + C5: еженедельное резюме (текст расширяется в C5); C1 — INSERT + SaveChanges до хаба.</summary>
    public async Task CreateWeeklySummaries()
    {
        var parentIds = await childRepository.DistinctParentIds();
        var created = new List<Notification>();
        foreach (var parentId in parentIds)
        {
            var n = new Notification
            {
                ParentId = parentId,
                Type = NotificationType.WeeklySummary,
                Title = "Weekly progress summary",
                Body = "Your weekly progress report is ready."
            };
            await notificationRepository.Add(n);
            created.Add(n);
        }

        await unitOfWork.SaveChanges();
        foreach (var n in created)
            await notificationPublisher.PublishSavedAsync(n);
    }
}
