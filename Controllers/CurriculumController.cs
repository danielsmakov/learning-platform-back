using LearningPlatform.Application;
using LearningPlatform.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LearningPlatform.Controllers;

[ApiController]
[Route("api/v1")]
public class CurriculumController(CurriculumService service) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("units")]
    public async Task<IActionResult> ListUnits([FromQuery] UnitQueryOptions query) => Ok(await service.GetUnits(query));

    [Authorize]
    [HttpPost("units")]
    public async Task<IActionResult> CreateUnit([FromBody] CreateUnitRequest request)
    {
        AuthGuard.RequireAdmin(User);
        return Created("/api/v1/units", await service.CreateUnit(request, AuthGuard.GetUserId(User)));
    }

    [Authorize]
    [HttpPut("units/{id:guid}")]
    public async Task<IActionResult> UpdateUnit(Guid id, [FromBody] UpdateUnitRequest request)
    {
        AuthGuard.RequireAdmin(User);
        return Ok(await service.UpdateUnit(id, request, AuthGuard.GetUserId(User)));
    }

    [Authorize]
    [HttpDelete("units/{id:guid}")]
    public async Task<IActionResult> DeleteUnit(Guid id)
    {
        AuthGuard.RequireAdmin(User);
        await service.DeleteUnit(id, AuthGuard.GetUserId(User));
        return NoContent();
    }

    [AllowAnonymous]
    [HttpGet("lessons")]
    public async Task<IActionResult> ListLessons([FromQuery] LessonQueryOptions query) => Ok(await service.GetLessons(query));

    [Authorize]
    [HttpPost("lessons")]
    public async Task<IActionResult> CreateLesson([FromBody] CreateLessonRequest request)
    {
        AuthGuard.RequireAdmin(User);
        return Created("/api/v1/lessons", await service.CreateLesson(request, AuthGuard.GetUserId(User)));
    }

    [Authorize]
    [HttpPut("lessons/{id:guid}")]
    public async Task<IActionResult> UpdateLesson(Guid id, [FromBody] UpdateLessonRequest request)
    {
        AuthGuard.RequireAdmin(User);
        return Ok(await service.UpdateLesson(id, request, AuthGuard.GetUserId(User)));
    }

    [Authorize]
    [HttpDelete("lessons/{id:guid}")]
    public async Task<IActionResult> DeleteLesson(Guid id)
    {
        AuthGuard.RequireAdmin(User);
        await service.DeleteLesson(id, AuthGuard.GetUserId(User));
        return NoContent();
    }

    [AllowAnonymous]
    [HttpGet("lessons/{id:guid}/exercises")]
    public async Task<IActionResult> ListExercises(Guid id, [FromQuery] QueryOptions query) => Ok(await service.GetExercises(id, query));

    [Authorize]
    [HttpPost("lessons/{id:guid}/exercises")]
    public async Task<IActionResult> CreateExercise(Guid id, [FromBody] CreateExerciseRequest request)
    {
        AuthGuard.RequireAdmin(User);
        return Created($"/api/v1/lessons/{id}/exercises", await service.CreateExercise(id, request, AuthGuard.GetUserId(User)));
    }

    [Authorize]
    [HttpPut("exercises/{id:guid}")]
    public async Task<IActionResult> UpdateExercise(Guid id, [FromBody] UpdateExerciseRequest request)
    {
        AuthGuard.RequireAdmin(User);
        return Ok(await service.UpdateExercise(id, request, AuthGuard.GetUserId(User)));
    }

    [Authorize]
    [HttpDelete("exercises/{id:guid}")]
    public async Task<IActionResult> DeleteExercise(Guid id)
    {
        AuthGuard.RequireAdmin(User);
        await service.DeleteExercise(id, AuthGuard.GetUserId(User));
        return NoContent();
    }
}
