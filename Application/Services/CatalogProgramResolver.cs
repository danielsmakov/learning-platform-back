using System.Security.Claims;
using LearningPlatform.Infrastructure.Repositories;

namespace LearningPlatform.Application.Services;

/// <summary>Определяет ProgramId для выдачи каталога (D4): админ — явный programId; ребёнок — своя программа; родитель — childId или programId; аноним — programId.</summary>
public class CatalogProgramResolver(IChildRepository childRepository)
{
    public async Task<Guid> ResolveCatalogProgramIdAsync(ClaimsPrincipal user, Guid? programId, Guid? childId)
    {
        if (AuthGuard.IsAdmin(user))
        {
            if (!programId.HasValue || programId.Value == Guid.Empty)
                throw new InvalidOperationException("Query parameter programId is required for admin catalog.");
            return programId.Value;
        }

        if (AuthGuard.IsChild(user))
        {
            var tokenChildId = AuthGuard.GetUserId(user);
            var child = await childRepository.GetById(tokenChildId) ?? throw new KeyNotFoundException("Child not found.");
            return child.CurrentProgramId;
        }

        if (user.Identity?.IsAuthenticated == true && user.IsInRole("Parent"))
        {
            if (childId.HasValue)
            {
                var parentId = AuthGuard.GetUserId(user);
                if (!await childRepository.IsOwner(parentId, childId.Value))
                    throw new UnauthorizedAccessException("You do not have access to this child.");
                var child = await childRepository.GetById(childId.Value) ?? throw new KeyNotFoundException("Child not found.");
                return child.CurrentProgramId;
            }

            if (programId.HasValue && programId.Value != Guid.Empty)
                return programId.Value;

            throw new InvalidOperationException("Provide programId or childId for catalog.");
        }

        if (!programId.HasValue || programId.Value == Guid.Empty)
            throw new InvalidOperationException("Query parameter programId is required.");

        return programId.Value;
    }
}
