using LearningPlatform.Application;
using LearningPlatform.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LearningPlatform.Controllers;

[ApiController]
[Route("api/v1")]
public class StatsController(ParentChildService parentChildService, AdminService adminService) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("leaderboard")]
    public async Task<IActionResult> Leaderboard([FromQuery] LeaderboardQueryOptions query)
        => Ok(await parentChildService.GetLeaderboard(query));

    [Authorize(Roles = "Admin")]
    [HttpGet("admin/logs")]
    public async Task<IActionResult> AdminLogs([FromQuery] QueryOptions query)
        => Ok(await adminService.GetLogs(query));

    [Authorize(Roles = "Admin")]
    [HttpGet("admin/stats")]
    public async Task<IActionResult> AdminStats()
        => Ok(await adminService.GetStats());
}
