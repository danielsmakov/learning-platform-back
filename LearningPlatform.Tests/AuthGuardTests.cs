using System.Security.Claims;
using LearningPlatform.Application;
using LearningPlatform.Application.Services;
using Xunit;

namespace LearningPlatform.Tests;

public class AuthGuardTests
{
    [Fact]
    public void GetUserId_reads_NameIdentifier_claim()
    {
        var id = Guid.NewGuid();
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, id.ToString())],
            "Test"));

        Assert.Equal(id, AuthGuard.GetUserId(user));
    }

    [Fact]
    public void GetUserId_throws_when_claim_missing()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity([], "Test"));
        Assert.Throws<AppUnauthorizedException>(() => AuthGuard.GetUserId(user));
    }

    [Fact]
    public void RequireAdmin_throws_for_parent()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, "Parent")],
            "Test"));
        Assert.Throws<AppForbiddenException>(() => AuthGuard.RequireAdmin(user));
    }

    [Fact]
    public void RequireAdmin_succeeds_for_admin()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, "Admin")],
            "Test"));
        AuthGuard.RequireAdmin(user);
    }

    [Fact]
    public void RequireSelfOrAdmin_allows_admin_even_when_id_differs()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
            ],
            "Test"));
        AuthGuard.RequireSelfOrAdmin(user, Guid.NewGuid());
    }

    [Fact]
    public void RequireSelfOrAdmin_allows_matching_user_id()
    {
        var id = Guid.NewGuid();
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, id.ToString())],
            "Test"));
        AuthGuard.RequireSelfOrAdmin(user, id);
    }

    [Fact]
    public async Task RequireChildAccess_admin_skips_ownership_check()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
            ],
            "Test"));

        await AuthGuard.RequireChildAccess(user, null!, Guid.NewGuid());
    }

    [Fact]
    public async Task RequireChildAccess_child_allowed_for_own_id()
    {
        var childId = Guid.NewGuid();
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Role, "Child"),
                new Claim(ClaimTypes.NameIdentifier, childId.ToString())
            ],
            "Test"));

        await AuthGuard.RequireChildAccess(user, null!, childId);
    }

    [Fact]
    public async Task RequireChildAccess_child_forbidden_for_other_id()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Role, "Child"),
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
            ],
            "Test"));

        await Assert.ThrowsAsync<AppForbiddenException>(() =>
            AuthGuard.RequireChildAccess(user, null!, Guid.NewGuid()));
    }
}
