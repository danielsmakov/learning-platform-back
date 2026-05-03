using LearningPlatform.Domain;
using Microsoft.AspNetCore.SignalR;

namespace LearningPlatform.Application.Services;

/// <summary>G4 / C2: доставка в UI после записи в БД.</summary>
public interface IParentNotificationPublisher
{
    /// <summary>
    /// P3 / C1 + C2: вызывать только после <see cref="Infrastructure.Repositories.INotificationRepository.Add"/> и <see cref="Infrastructure.Repositories.IUnitOfWork.SaveChanges"/>.
    /// Родитель офлайн — история из <c>Notifications</c>; онлайн — тот же payload в хаб <see cref="ParentNotificationHub"/> (группа = <c>parentId</c>), без дублирующего INSERT.
    /// </summary>
    Task PublishSavedAsync(Notification notification);
}

/// <summary>C1/C2: только SignalR; INSERT и источник истины — БД на вызывающей стороне.</summary>
public class ParentNotificationPublisher(IHubContext<ParentNotificationHub> hubContext) : IParentNotificationPublisher
{
    public const string HubEventName = "notification";

    /// <inheritdoc />
    public Task PublishSavedAsync(Notification notification) =>
        hubContext.Clients.Group(notification.ParentId.ToString("D")).SendAsync(HubEventName, ParentNotificationPayload.From(notification));

    /// <summary>Согласованный с БД payload для клиента (типы = <see cref="NotificationType"/>).</summary>
    public sealed record ParentNotificationPayload(Guid Id, NotificationType Type, string Title, string Body, Guid? ChildId, DateTime CreatedAt, bool IsRead)
    {
        public static ParentNotificationPayload From(Notification n) =>
            new(n.Id, n.Type, n.Title, n.Body, n.ChildId, n.CreatedAt, n.IsRead);
    }
}
