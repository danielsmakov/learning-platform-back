using LearningPlatform.Application;
using LearningPlatform.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LearningPlatform.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController(AuthService authService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await authService.RegisterParent(request);
        return Created($"/api/v1/parents/{result.UserId}", result);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var tokens = await authService.Login(request);
        return Ok(tokens);
    }

    [AllowAnonymous]
    [HttpPost("child-login")]
    public async Task<IActionResult> ChildLogin([FromBody] ChildLoginRequest request)
    {
        var tokens = await authService.ChildLogin(request);
        return Ok(tokens);
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        var tokens = await authService.Refresh(request.RefreshToken);
        return Ok(tokens);
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshRequest request)
    {
        await authService.Logout(request.RefreshToken);
        return NoContent();
    }
}
