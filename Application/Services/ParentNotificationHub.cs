using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace LearningPlatform.Application.Services;

/// <summary>
/// H3: только роль Parent и валидный JWT; группа SignalR = <c>parentId</c> (для push уведомлений).
/// Клиент: при negotiate/WebSocket передать токен в query <c>access_token</c> или заголовок <c>Authorization: Bearer</c> (см. JwtBearer OnMessageReceived в Program.cs).
/// Событие входящих сообщений: <see cref="ParentNotificationPublisher.HubEventName"/> (<c>notification</c>); контракт SignalR — в тексте OpenAPI (Swagger) API v1.
/// </summary>
[Authorize(Roles = "Parent")]
public class ParentNotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var parentId = AuthGuard.GetUserId(Context.User!);
        await Groups.AddToGroupAsync(Context.ConnectionId, parentId.ToString("D"));
        await base.OnConnectedAsync();
    }
}
