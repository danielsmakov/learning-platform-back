using Hangfire;

namespace LearningPlatform.Application.Services;

/// <summary>E5: сначала бейджи по метрикам завершённого юнита, затем адаптивная смена программы (порядок важен — ErrorCount в ChildUnitProgress).</summary>
public class UnitCompletionFollowUpJob(BadgeEvaluationJob badgeEvaluationJob, AdaptiveDifficultyJob adaptiveDifficultyJob)
{
    [AutomaticRetry(Attempts = 2)]
    public async Task RunAsync(Guid childId, Guid unitId)
    {
        await badgeEvaluationJob.EvaluateAfterUnitCompletionAsync(childId, unitId);
        await adaptiveDifficultyJob.EvaluateCompletedUnitAsync(childId, unitId);
    }
}
