using LearningPlatform.Application;
using LearningPlatform.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LearningPlatform.Controllers;

[ApiController]
[Authorize(Roles = "Parent,Admin")]
[Route("api/v1/parents")]
public class ParentsController(ParentChildService service) : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetParent(Guid id)
    {
        AuthGuard.RequireSelfOrAdmin(User, id);
        return Ok(await service.GetParent(id));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateParent(Guid id, [FromBody] UpdateParentRequest request)
    {
        AuthGuard.RequireSelfOrAdmin(User, id);
        return Ok(await service.UpdateParent(id, request));
    }

    [HttpGet("{id:guid}/children")]
    public async Task<IActionResult> GetParentChildren(Guid id, [FromQuery] QueryOptions query)
    {
        AuthGuard.RequireSelfOrAdmin(User, id);
        return Ok(await service.GetParentChildren(id, query));
    }
}
