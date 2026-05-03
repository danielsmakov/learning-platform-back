using Hangfire;
using LearningPlatform.Application;
using LearningPlatform.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LearningPlatform.Controllers;

[ApiController]
[Authorize]
[Route("api/v1")]
public class LearningController(
    LearningService service,
    ParentChildService childService,
    IBackgroundJobClient jobs) : ControllerBase
{
    [HttpPost("exercises/{id:guid}/submit")]
    public async Task<IActionResult> SubmitExercise(Guid id, [FromBody] SubmitExerciseRequest request)
    {
        await AuthGuard.RequireChildAccess(User, childService, request.ChildId);
        return Ok(await service.SubmitExercise(id, request));
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
