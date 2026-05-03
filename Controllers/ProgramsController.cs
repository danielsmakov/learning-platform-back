using LearningPlatform.Application;
using LearningPlatform.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LearningPlatform.Controllers;

/// <summary>H3: чтение списка программ — аноним; мутации — только Admin.</summary>
[ApiController]
[Route("api/v1/programs")]
public class ProgramsController(CurriculumService curriculum) : ControllerBase
{
    /// <summary>Список программ (G2: Accept-Language). По умолчанию только опубликованные; all=true — только админ, все строки.</summary>
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> ListPrograms([FromQuery] QueryOptions query, [FromQuery] bool all = false,
        [FromHeader(Name = "Accept-Language")] string? acceptLanguage = null)
    {
        if (all)
            AuthGuard.RequireAdmin(User);
        return Ok(await curriculum.GetPrograms(query, includeUnpublished: all, acceptLanguage: acceptLanguage));
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
