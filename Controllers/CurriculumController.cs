using LearningPlatform.Application;
using LearningPlatform.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LearningPlatform.Controllers;

[ApiController]
[Route("api/v1")]
public class CurriculumController(CurriculumService service, CatalogProgramResolver catalogResolver) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("units")]
    public async Task<IActionResult> ListUnits([FromQuery] UnitQueryOptions query)
    {
        var programId = await catalogResolver.ResolveCatalogProgramIdAsync(User, query.ProgramId, query.ChildId);
        query.ProgramId = programId;
        return Ok(await service.GetUnits(query));
    }

    [Authorize]
    [HttpPost("units")]
    public async Task<IActionResult> CreateUnit([FromQuery] Guid programId, [FromBody] CreateUnitRequest request)
    {
        AuthGuard.RequireAdmin(User);
        return Created("/api/v1/units", await service.CreateUnit(request, AuthGuard.GetUserId(User), programId));
    }

    [Authorize]
    [HttpPut("units/{id:guid}")]
    public async Task<IActionResult> UpdateUnit(Guid id, [FromQuery] Guid programId, [FromBody] UpdateUnitRequest request)
    {
        AuthGuard.RequireAdmin(User);
        return Ok(await service.UpdateUnit(id, request, AuthGuard.GetUserId(User), programId));
    }

    [Authorize]
    [HttpDelete("units/{id:guid}")]
    public async Task<IActionResult> DeleteUnit(Guid id, [FromQuery] Guid programId)
    {
        AuthGuard.RequireAdmin(User);
        await service.DeleteUnit(id, AuthGuard.GetUserId(User), programId);
        return NoContent();
    }

    [AllowAnonymous]
    [HttpGet("lessons")]
    public async Task<IActionResult> ListLessons([FromQuery] LessonQueryOptions query)
    {
        var programId = await catalogResolver.ResolveCatalogProgramIdAsync(User, query.ProgramId, query.ChildId);
        query.ProgramId = programId;
        return Ok(await service.GetLessons(query));
    }

    [Authorize]
    [HttpPost("lessons")]
    public async Task<IActionResult> CreateLesson([FromQuery] Guid programId, [FromBody] CreateLessonRequest request)
    {
        AuthGuard.RequireAdmin(User);
        return Created("/api/v1/lessons", await service.CreateLesson(request, AuthGuard.GetUserId(User), programId));
    }

    [Authorize]
    [HttpPut("lessons/{id:guid}")]
    public async Task<IActionResult> UpdateLesson(Guid id, [FromQuery] Guid programId, [FromBody] UpdateLessonRequest request)
    {
        AuthGuard.RequireAdmin(User);
        return Ok(await service.UpdateLesson(id, request, AuthGuard.GetUserId(User), programId));
    }

    [Authorize]
    [HttpDelete("lessons/{id:guid}")]
    public async Task<IActionResult> DeleteLesson(Guid id, [FromQuery] Guid programId)
    {
        AuthGuard.RequireAdmin(User);
        await service.DeleteLesson(id, AuthGuard.GetUserId(User), programId);
        return NoContent();
    }

    [AllowAnonymous]
    [HttpGet("lessons/{id:guid}/exercises")]
    public async Task<IActionResult> ListExercises(Guid id, [FromQuery] QueryOptions query, [FromQuery] Guid? programId, [FromQuery] Guid? childId)
    {
        var resolved = await catalogResolver.ResolveCatalogProgramIdAsync(User, programId, childId);
        await service.EnsureLessonBelongsToProgram(id, resolved);
        return Ok(await service.GetExercises(id, query));
    }

    [Authorize]
    [HttpPost("lessons/{id:guid}/exercises")]
    public async Task<IActionResult> CreateExercise(Guid id, [FromQuery] Guid programId, [FromBody] CreateExerciseRequest request)
    {
        AuthGuard.RequireAdmin(User);
        return Created($"/api/v1/lessons/{id}/exercises", await service.CreateExercise(id, request, AuthGuard.GetUserId(User), programId));
    }

    [Authorize]
    [HttpPut("exercises/{id:guid}")]
    public async Task<IActionResult> UpdateExercise(Guid id, [FromQuery] Guid programId, [FromBody] UpdateExerciseRequest request)
    {
        AuthGuard.RequireAdmin(User);
        return Ok(await service.UpdateExercise(id, request, AuthGuard.GetUserId(User), programId));
    }

    [Authorize]
    [HttpDelete("exercises/{id:guid}")]
    public async Task<IActionResult> DeleteExercise(Guid id, [FromQuery] Guid programId)
    {
        AuthGuard.RequireAdmin(User);
        await service.DeleteExercise(id, AuthGuard.GetUserId(User), programId);
        return NoContent();
    }
}
