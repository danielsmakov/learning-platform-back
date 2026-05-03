using LearningPlatform.Application;
using LearningPlatform.Domain;
using LearningPlatform.Infrastructure.Repositories;

namespace LearningPlatform.Application.Services;

public class ParentChildService(
    IUserRepository userRepository,
    IChildRepository childRepository,
    ILearningRepository learningRepository,
    IBadgeRepository badgeRepository,
    IUnitOfWork unitOfWork)
{
    public async Task<User> GetParent(Guid id) => await userRepository.GetById(id) ?? throw new KeyNotFoundException("Parent not found.");

    public async Task<User> UpdateParent(Guid id, UpdateParentRequest request)
    {
        var parent = await userRepository.GetById(id) ?? throw new KeyNotFoundException("Parent not found.");
        parent.Email = request.Email.Trim().ToLowerInvariant();
        await unitOfWork.SaveChanges();
        return parent;
    }

    public Task<PagedResponse<Child>> GetParentChildren(Guid parentId, QueryOptions query) => childRepository.GetByParent(parentId, query);

    public async Task<Child> CreateChild(CreateChildRequest request)
    {
        var child = new Child
        {
            ParentId = request.ParentId,
            Name = request.Name,
            Age = request.Age,
            AvatarUrl = request.AvatarUrl,
            DisplayName = request.DisplayName,
            PinHash = BCrypt.Net.BCrypt.HashPassword(request.Pin, workFactor: 12)
        };
        await childRepository.Add(child);
        await unitOfWork.SaveChanges();
        return child;
    }

    public async Task<Child> GetChild(Guid id) => await childRepository.GetById(id) ?? throw new KeyNotFoundException("Child not found.");

    public async Task<Child> UpdateChild(Guid id, UpdateChildRequest request)
    {
        var child = await childRepository.GetById(id) ?? throw new KeyNotFoundException("Child not found.");
        child.Name = request.Name;
        child.Age = request.Age;
        child.AvatarUrl = request.AvatarUrl;
        child.DisplayName = request.DisplayName;
        await unitOfWork.SaveChanges();
        return child;
    }

    public async Task DeleteChild(Guid id)
    {
        var child = await childRepository.GetById(id) ?? throw new KeyNotFoundException("Child not found.");
        await childRepository.Delete(child);
        await unitOfWork.SaveChanges();
    }

    public async Task<bool> IsOwner(Guid parentId, Guid childId)
    {
        return await childRepository.IsOwner(parentId, childId);
    }

    public Task<PagedResponse<ChildLessonProgress>> GetChildProgress(Guid childId, QueryOptions query) => learningRepository.GetProgressByChild(childId, query);

    public Task<PagedResponse<object>> GetChildBadges(Guid childId, QueryOptions query) => badgeRepository.GetChildBadges(childId, query);

    public Task<PagedResponse<object>> GetLeaderboard(LeaderboardQueryOptions query) => childRepository.GetLeaderboard(query);
}
