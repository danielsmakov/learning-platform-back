using System.Text;
using LearningPlatform.Application;
using LearningPlatform.Domain;
using LearningPlatform.Infrastructure.Repositories;

namespace LearningPlatform.Application.Services;

public class NotificationService(
    INotificationRepository notificationRepository,
    IChildRepository childRepository,
    ILearningRepository learningRepository,
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
                Body = $"{child.Name} has not practiced today yet."
            };
            await notificationRepository.Add(n);
            created.Add(n);
        }

        await unitOfWork.SaveChanges();
        foreach (var n in created)
            await notificationPublisher.PublishSavedAsync(n);
    }

    /// <summary>P3 / C3 + C5: еженедельное резюме с агрегатами; C1/C2 — INSERT + SaveChanges, затем SignalR.</summary>
    public async Task CreateWeeklySummaries()
    {
        var endUtc = DateTime.UtcNow;
        var startUtc = endUtc.AddDays(-7);
        var parentIds = await childRepository.DistinctParentIds();
        var created = new List<Notification>();
        foreach (var parentId in parentIds)
        {
            var children = await childRepository.ListChildrenForParentAsync(parentId);
            if (children.Count == 0)
                continue;

            var lines = new List<(Child Child, WeeklyActivityStats Stats)>();
            foreach (var child in children)
            {
                var stats = await learningRepository.GetWeeklyActivityStatsAsync(child.Id, startUtc, endUtc);
                lines.Add((child, stats));
            }

            var body = BuildWeeklySummaryBody(startUtc, endUtc, lines);
            var n = new Notification
            {
                ParentId = parentId,
                Type = NotificationType.WeeklySummary,
                Title = $"Weekly summary ({startUtc:yyyy-MM-dd} – {endUtc:yyyy-MM-dd} UTC)",
                Body = body
            };
            await notificationRepository.Add(n);
            created.Add(n);
        }

        await unitOfWork.SaveChanges();
        foreach (var n in created)
            await notificationPublisher.PublishSavedAsync(n);
    }

    private static string BuildWeeklySummaryBody(DateTime startUtc, DateTime endUtc, IReadOnlyList<(Child Child, WeeklyActivityStats Stats)> rows)
    {
        var sb = new StringBuilder();
        sb.Append("Rolling 7 days (UTC). Metrics: exercise attempts (incorrect), lessons completed, units completed, distinct lessons with submissions, distinct units touched.");
        sb.AppendLine();
        sb.AppendLine($"Window: [{startUtc:yyyy-MM-dd HH:mm} – {endUtc:yyyy-MM-dd HH:mm}).");

        foreach (var (child, s) in rows)
        {
            sb.AppendLine();
            sb.Append(child.Name);
            sb.Append(": ");
            sb.Append($"{s.ExerciseSubmissions} attempts ({s.IncorrectSubmissions} incorrect), ");
            sb.Append($"{s.LessonsCompletedInPeriod} lessons completed, ");
            sb.Append($"{s.UnitsCompletedInPeriod} units completed, ");
            sb.Append($"{s.DistinctLessonsWithSubmissions} lessons w/ activity, ");
            sb.Append($"{s.DistinctUnitsTouched} units touched.");
        }

        var text = sb.ToString();
        const int maxLen = 2000;
        return text.Length <= maxLen ? text : text[..(maxLen - 3)] + "...";
    }
}
