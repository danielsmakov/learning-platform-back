using LearningPlatform.Application;
using LearningPlatform.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LearningPlatform.Controllers;

[ApiController]
[Route("api/v1/programs")]
public class ProgramsController(CurriculumService curriculum) : ControllerBase
{
    /// <summary>Список программ. По умолчанию только опубликованные; all=true — только админ, все строки.</summary>
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> ListPrograms([FromQuery] QueryOptions query, [FromQuery] bool all = false)
    {
        if (all)
            AuthGuard.RequireAdmin(User);
        return Ok(await curriculum.GetPrograms(query, includeUnpublished: all));
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreateProgram([FromBody] CreateLearningProgramRequest request)
    {
        AuthGuard.RequireAdmin(User);
        var created = await curriculum.CreateProgram(request, AuthGuard.GetUserId(User));
        return Created($"/api/v1/programs/{created.Id}", created);
    }

    [Authorize]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateProgram(Guid id, [FromBody] UpdateLearningProgramRequest request)
    {
        AuthGuard.RequireAdmin(User);
        return Ok(await curriculum.UpdateProgram(id, request, AuthGuard.GetUserId(User)));
    }

    [Authorize]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteProgram(Guid id)
    {
        AuthGuard.RequireAdmin(User);
        await curriculum.DeleteProgram(id, AuthGuard.GetUserId(User));
        return NoContent();
    }
}
