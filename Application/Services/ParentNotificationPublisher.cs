using LearningPlatform.Domain;
using Microsoft.AspNetCore.SignalR;

namespace LearningPlatform.Application.Services;

/// <summary>G4 / C2: единый push в SignalR после сохранения записи в <see cref="Notification"/>.</summary>
public interface IParentNotificationPublisher
{
    /// <summary>Событие хаба и контракт payload совпадают для всех типов <see cref="NotificationType"/>.</summary>
    Task PublishSavedAsync(Notification notification);
}

public class ParentNotificationPublisher(IHubContext<ParentNotificationHub> hubContext) : IParentNotificationPublisher
{
    public const string HubEventName = "notification";

    public Task PublishSavedAsync(Notification notification) =>
        hubContext.Clients.Group(notification.ParentId.ToString("D")).SendAsync(HubEventName, ParentNotificationPayload.From(notification));

    /// <summary>Согласованный с БД payload для клиента (типы = <see cref="NotificationType"/>).</summary>
    public sealed record ParentNotificationPayload(Guid Id, NotificationType Type, string Title, string Body, Guid? ChildId, DateTime CreatedAt, bool IsRead)
    {
        public static ParentNotificationPayload From(Notification n) =>
            new(n.Id, n.Type, n.Title, n.Body, n.ChildId, n.CreatedAt, n.IsRead);
    }
}
