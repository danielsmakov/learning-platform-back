using LearningPlatform.Application;
using LearningPlatform.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LearningPlatform.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/children")]
public class ChildrenController(ParentChildService service, CurriculumMapService curriculumMapService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> ListChildren([FromQuery] Guid parentId, [FromQuery] QueryOptions query)
    {
        AuthGuard.RequireParentOrAdmin(User);
        if (!AuthGuard.IsAdmin(User) && AuthGuard.GetUserId(User) != parentId)
        {
            throw new UnauthorizedAccessException("Forbidden.");
        }

        return Ok(await service.GetParentChildren(parentId, query));
    }

    [HttpPost]
    public async Task<IActionResult> CreateChild([FromBody] CreateChildRequest request)
    {
        AuthGuard.RequireParentOrAdmin(User);
        if (!AuthGuard.IsAdmin(User))
        {
            request = request with { ParentId = AuthGuard.GetUserId(User) };
        }

        var adminActor = AuthGuard.IsAdmin(User) ? AuthGuard.GetUserId(User) : (Guid?)null;
        var child = await service.CreateChild(request, adminActor);
        return Created($"/api/v1/children/{child.Id}", child);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetChild(Guid id)
    {
        await AuthGuard.RequireChildAccess(User, service, id);
        return Ok(await service.GetChild(id));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateChild(Guid id, [FromBody] UpdateChildRequest request)
    {
        await AuthGuard.RequireChildAccess(User, service, id);
        var adminActor = AuthGuard.IsAdmin(User) ? AuthGuard.GetUserId(User) : (Guid?)null;
        return Ok(await service.UpdateChild(id, request, adminActor));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteChild(Guid id)
    {
        await AuthGuard.RequireChildAccess(User, service, id);
        var adminActor = AuthGuard.IsAdmin(User) ? AuthGuard.GetUserId(User) : (Guid?)null;
        await service.DeleteChild(id, adminActor);
        return NoContent();
    }

    [HttpGet("{id:guid}/progress")]
    public async Task<IActionResult> GetChildProgress(Guid id, [FromQuery] QueryOptions query)
    {
        await AuthGuard.RequireChildAccess(User, service, id);
        return Ok(await service.GetChildProgress(id, query));
    }

    [HttpGet("{id:guid}/badges")]
    public async Task<IActionResult> GetChildBadges(Guid id, [FromQuery] QueryOptions query)
    {
        await AuthGuard.RequireChildAccess(User, service, id);
        return Ok(await service.GetChildBadges(id, query));
    }

    /// <summary>B1: карта куррикулума ребёнка. B6/G2: строки контента по заголовку Accept-Language (ru/en).</summary>
    [HttpGet("{id:guid}/curriculum-map")]
    public async Task<IActionResult> GetCurriculumMap(Guid id, [FromHeader(Name = "Accept-Language")] string? acceptLanguage = null)
    {
        await AuthGuard.RequireChildAccess(User, service, id);
        var locale = LocalePreference.Parse(acceptLanguage);
        return Ok(await curriculumMapService.GetMapAsync(id, locale));
    }
}
