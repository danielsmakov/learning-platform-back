using LearningPlatform.Application;
using LearningPlatform.Infrastructure.Repositories;

namespace LearningPlatform.Application.Services;

public class AdminService(IActivityLogRepository activityLogRepository, IUserRepository userRepository, IChildRepository childRepository, ILearningRepository learningRepository)
{
    public Task<PagedResponse<object>> GetLogs(QueryOptions query) => activityLogRepository.GetLogs(query);

    public async Task<object> GetStats()
    {
        var totalParents = await userRepository.CountParents();
        var totalChildren = await childRepository.CountAll();
        var completedLessons = await learningRepository.CountCompletedLessonsAll();
        var totalProgressRows = await learningRepository.CountProgressRows();
        var completionRate = totalProgressRows == 0 ? 0 : Math.Round(completedLessons * 100.0 / totalProgressRows, 2);
        return new { totalParents, totalChildren, completedLessons, completionRate };
    }
}
