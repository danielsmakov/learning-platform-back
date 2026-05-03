using LearningPlatform.Application;
using LearningPlatform.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LearningPlatform.Controllers;

/// <summary>
/// Родительские уведомления: только JWT роли Parent (H3). Ребёнок и админ здесь не поддерживаются —
/// уведомления привязаны к <c>ParentId</c> в БД; админ смотрит данные через БД или отдельные админ-эндпоинты при появлении.
/// </summary>
[ApiController]
[Authorize(Roles = "Parent")]
[Route("api/v1/notifications")]
public class NotificationsController(NotificationService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> ListNotifications([FromQuery] QueryOptions query)
        => Ok(await service.GetNotifications(AuthGuard.GetUserId(User), query));

    [HttpPatch]
    public async Task<IActionResult> MarkRead([FromBody] MarkNotificationsReadRequest request)
    {
        await service.MarkRead(AuthGuard.GetUserId(User), request.NotificationIds);
        return NoContent();
    }
}
