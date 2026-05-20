using LearningPlatform.Application;
using LearningPlatform.Domain;
using LearningPlatform.Infrastructure.Repositories;

namespace LearningPlatform.Application.Services;

public class ParentChildService(
    IUserRepository userRepository,
    IChildRepository childRepository,
    ICurriculumRepository curriculumRepository,
    ILearningRepository learningRepository,
    IBadgeRepository badgeRepository,
    IActivityLogRepository activityLogRepository,
    IUnitOfWork unitOfWork,
    CurrentUnitProgressService currentUnitProgress)
{
    public async Task<User> GetParent(Guid id) => await userRepository.GetById(id) ?? throw new KeyNotFoundException("Parent not found.");

    public async Task<User> GetParentByEmail(string email)
    {
        var normalized = email.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(normalized))
            throw new KeyNotFoundException("Parent not found.");

        var user = await userRepository.GetByEmail(normalized);
        if (user is null || user.Role != UserRole.Parent)
            throw new KeyNotFoundException("Parent not found.");

        return user;
    }

    public async Task<User> UpdateParent(Guid id, UpdateParentRequest request)
    {
        var parent = await userRepository.GetById(id) ?? throw new KeyNotFoundException("Parent not found.");
        parent.Email = request.Email.Trim().ToLowerInvariant();
        await unitOfWork.SaveChanges();
        return parent;
    }

    public async Task ChangeParentPassword(Guid id, ChangeParentPasswordRequest request)
    {
        var parent = await userRepository.GetById(id) ?? throw new KeyNotFoundException("Parent not found.");
        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, parent.PasswordHash))
            throw new InvalidOperationException("Current password is incorrect.");

        parent.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, workFactor: 12);
        await unitOfWork.SaveChanges();
    }

    public async Task<PagedResponse<ChildResponse>> GetParentChildren(Guid parentId, QueryOptions query)
    {
        var page = await childRepository.GetByParent(parentId, query);
        var items = new List<ChildResponse>();
        foreach (var child in page.Items)
            items.Add(await BuildChildResponseAsync(child));
        return new PagedResponse<ChildResponse>
        {
            Items = items,
            Total = page.Total,
            Page = page.Page,
            PageSize = page.PageSize,
            TotalPages = page.TotalPages
        };
    }

    public async Task<ChildResponse> CreateChild(CreateChildRequest request, Guid? adminActorId = null)
    {
        var program = await curriculumRepository.GetProgramByTrack(request.LearningProgramTrack)
            ?? throw new KeyNotFoundException("Program for selected level not found.");
        var login = ChildLoginRules.NormalizeLogin(request.Login);
        if (await childRepository.LoginExistsAsync(login))
            throw new InvalidOperationException("Login is already taken.");

        var child = new Child
        {
            ParentId = request.ParentId,
            Name = request.Name.Trim(),
            Age = request.Age,
            AvatarUrl = ChildAvatars.Normalize(request.AvatarUrl),
            Login = login,
            PinHash = BCrypt.Net.BCrypt.HashPassword(request.Pin, workFactor: 12),
            CurrentProgramId = program.Id
        };
        await childRepository.Add(child);
        if (adminActorId.HasValue)
            await LogAdminWrite(adminActorId.Value, "create", "child", child.Id.ToString());
        await unitOfWork.SaveChanges();
        var loaded = await childRepository.GetById(child.Id) ?? throw new InvalidOperationException("Child not found after create.");
        return await BuildChildResponseAsync(loaded);
    }

    public async Task<ChildResponse> GetChild(Guid id)
    {
        var child = await childRepository.GetById(id) ?? throw new KeyNotFoundException("Child not found.");
        return await BuildChildResponseAsync(child);
    }

    public async Task<ChildResponse> UpdateChild(Guid id, UpdateChildRequest request, Guid? adminActorId = null)
    {
        var child = await childRepository.GetById(id) ?? throw new KeyNotFoundException("Child not found.");
        var login = ChildLoginRules.NormalizeLogin(request.Login);
        if (await childRepository.LoginExistsAsync(login, id))
            throw new InvalidOperationException("Login is already taken.");

        child.Name = request.Name.Trim();
        child.Age = request.Age;
        child.AvatarUrl = ChildAvatars.Normalize(request.AvatarUrl);
        child.Login = login;
        if (request.LearningProgramTrack.HasValue)
        {
            var program = await curriculumRepository.GetProgramByTrack(request.LearningProgramTrack.Value)
                ?? throw new KeyNotFoundException("Program for selected level not found.");
            child.CurrentProgramId = program.Id;
        }

        if (adminActorId.HasValue)
            await LogAdminWrite(adminActorId.Value, "update", "child", id.ToString());
        await unitOfWork.SaveChanges();
        var reloaded = await childRepository.GetById(id) ?? throw new KeyNotFoundException("Child not found.");
        return await BuildChildResponseAsync(reloaded);
    }

    private async Task<ChildResponse> BuildChildResponseAsync(Child child)
    {
        if (child.CurrentProgram is null)
            throw new InvalidOperationException("Child.CurrentProgram must be loaded.");

        var (percent, currentUnitId) = await currentUnitProgress.ComputeAsync(child.Id, child.CurrentProgramId);

        return new ChildResponse(
            child.Id,
            child.ParentId,
            child.Name,
            child.Age,
            child.AvatarUrl,
            child.Login,
            child.CurrentLevel,
            child.XpTotal,
            child.StreakCurrent,
            child.StreakLongest,
            child.LastActivityDate,
            child.CreatedAt,
            child.CurrentProgramId,
            child.CurrentProgram.DifficultyTrack,
            percent,
            currentUnitId);
    }

    public async Task DeleteChild(Guid id, Guid? adminActorId = null)
    {
        var child = await childRepository.GetById(id) ?? throw new KeyNotFoundException("Child not found.");
        if (adminActorId.HasValue)
            await LogAdminWrite(adminActorId.Value, "delete", "child", id.ToString());
        await childRepository.Delete(child);
        await unitOfWork.SaveChanges();
    }

    private Task LogAdminWrite(Guid adminId, string action, string resourceType, string resourceId) =>
        activityLogRepository.Add(new ActivityLog
        {
            AdminId = adminId,
            Action = action,
            ResourceType = resourceType,
            ResourceId = resourceId
        });

    public async Task<bool> IsOwner(Guid parentId, Guid childId)
    {
        return await childRepository.IsOwner(parentId, childId);
    }

    public Task<PagedResponse<ChildLessonProgress>> GetChildProgress(Guid childId, QueryOptions query) => learningRepository.GetProgressByChild(childId, query);

    public Task<PagedResponse<object>> GetChildBadges(Guid childId, QueryOptions query) => badgeRepository.GetChildBadges(childId, query);

    public Task<PagedResponse<object>> GetLeaderboard(LeaderboardQueryOptions query) => childRepository.GetLeaderboard(query);
}
