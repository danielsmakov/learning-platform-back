using Hangfire;
using LearningPlatform.Application;
using LearningPlatform.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LearningPlatform.Controllers;

/// <summary>
/// H3: JWT Parent, Child или Admin; сабмит/resume/complete только с <see cref="AuthGuard.RequireChildAccess"/> (ребёнок — свой профиль, без админки каталога).
/// </summary>
[ApiController]
[Authorize(Roles = "Parent,Child,Admin")]
[Route("api/v1")]
public class LearningController(
    LearningService service,
    ParentChildService childService,
    IBackgroundJobClient jobs) : ControllerBase
{
    [EnableRateLimiting("submit")]
    [HttpPost("exercises/{id:guid}/submit")]
    public async Task<IActionResult> SubmitExercise(Guid id, [FromBody] SubmitExerciseRequest request)
    {
        await AuthGuard.RequireChildAccess(User, childService, request.ChildId);
        var response = await service.SubmitExercise(id, request);
        if (response.LessonJustCompleted)
            jobs.Enqueue<BadgeEvaluationJob>(x => x.Evaluate(request.ChildId));
        return Ok(response);
    }

    [HttpGet("lessons/{id:guid}/resume")]
    public async Task<IActionResult> GetLessonResume(Guid id, [FromQuery] Guid childId)
    {
        await AuthGuard.RequireChildAccess(User, childService, childId);
        return Ok(await service.GetLessonResume(id, childId));
    }

    [HttpPost("lessons/{id:guid}/complete")]
    public async Task<IActionResult> CompleteLesson(Guid id, [FromBody] CompleteLessonRequest request)
    {
        await AuthGuard.RequireChildAccess(User, childService, request.ChildId);
        var result = await service.CompleteLesson(id, request.ChildId);
        jobs.Enqueue<BadgeEvaluationJob>(x => x.Evaluate(request.ChildId));
        return Ok(result);
    }
}
