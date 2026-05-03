using System.Security.Claims;
using LearningPlatform.Application;

namespace LearningPlatform.Application.Services;

public static class AuthGuard
{
    public static Guid GetUserId(ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue(ClaimTypes.Name);
        if (!Guid.TryParse(value, out var id))
        {
            throw new AppUnauthorizedException("Invalid user identity.");
        }

        return id;
    }

    public static bool IsAdmin(ClaimsPrincipal user) => user.IsInRole("Admin");
    public static bool IsChild(ClaimsPrincipal user) => user.IsInRole("Child");
    public static bool IsParent(ClaimsPrincipal user) => user.IsInRole("Parent");

    /// <summary>H3: endpoints только для родителя (не Child, не аноним).</summary>
    public static void RequireParent(ClaimsPrincipal user)
    {
        if (!IsParent(user)) throw new AppForbiddenException("Parent role required.");
    }

    /// <summary>H3: явный запрет JWT ребёнка (админка, родительские данные и т.д.).</summary>
    public static void RequireNotChild(ClaimsPrincipal user)
    {
        if (IsChild(user)) throw new AppForbiddenException("Child accounts cannot access this resource.");
    }

    public static void RequireAdmin(ClaimsPrincipal user)
    {
        if (!IsAdmin(user)) throw new AppForbiddenException("Admin role required.");
    }

    public static void RequireParentOrAdmin(ClaimsPrincipal user)
    {
        if (user.IsInRole("Parent") || user.IsInRole("Admin")) return;
        throw new AppForbiddenException("Parent or admin role required.");
    }

    public static void RequireSelfOrAdmin(ClaimsPrincipal user, Guid userId)
    {
        if (IsAdmin(user) || GetUserId(user) == userId) return;
        throw new AppForbiddenException("Forbidden.");
    }

    public static async Task RequireChildAccess(ClaimsPrincipal user, ParentChildService service, Guid childId)
    {
        if (IsAdmin(user)) return;
        if (IsChild(user))
        {
            if (GetUserId(user) == childId) return;
            throw new AppForbiddenException("Forbidden child access.");
        }
        var currentUserId = GetUserId(user);
        var hasAccess = await service.IsOwner(currentUserId, childId);
        if (!hasAccess) throw new AppForbiddenException("Forbidden child access.");
    }
}
