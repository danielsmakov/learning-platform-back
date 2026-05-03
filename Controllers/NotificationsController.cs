using LearningPlatform.Application;
using LearningPlatform.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LearningPlatform.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/notifications")]
public class NotificationsController(NotificationService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> ListNotifications([FromQuery] QueryOptions query)
    {
        AuthGuard.RequireParentOrAdmin(User);
        return Ok(await service.GetNotifications(AuthGuard.GetUserId(User), query));
    }

    [HttpPatch]
    public async Task<IActionResult> MarkRead([FromBody] MarkNotificationsReadRequest request)
    {
        AuthGuard.RequireParentOrAdmin(User);
        await service.MarkRead(AuthGuard.GetUserId(User), request.NotificationIds);
        return NoContent();
    }
}
