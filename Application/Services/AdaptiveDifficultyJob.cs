using Hangfire;
using LearningPlatform.Application;
using LearningPlatform.Domain;
using LearningPlatform.Infrastructure.Repositories;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace LearningPlatform.Application.Services;

/// <summary>E4: после завершения юнита и по расписанию — пороги ошибок, смена программы, уведомление и SignalR.</summary>
public class AdaptiveDifficultyJob(
    IOptions<AdaptiveErrorsOptions> optionsAccessor,
    ILearningRepository learningRepository,
    IChildRepository childRepository,
    ICurriculumRepository curriculumRepository,
    INotificationRepository notificationRepository,
    IUnitOfWork unitOfWork,
    IHubContext<ParentNotificationHub> hubContext)
{
    private AdaptiveErrorsOptions Options => optionsAccessor.Value;

    /// <summary>Точка входа после того, как юнит впервые переведён в завершённый (ставится в очередь из учебного потока).</summary>
    [AutomaticRetry(Attempts = 3)]
    public Task EvaluateCompletedUnitAsync(Guid childId, Guid unitId) =>
        EvaluateCoreAsync(childId, unitId);

    /// <summary>Подбор необработанных завершённых юнитов (если фоновая задача не отработала сразу).</summary>
    [AutomaticRetry(Attempts = 2)]
    public async Task ProcessScheduledCompletedUnitsAsync()
    {
        var rows = await learningRepository.GetCompletedChildUnitProgressRowsAsync();
        foreach (var row in rows)
            await EvaluateCoreAsync(row.ChildId, row.UnitId);
    }

    private async Task EvaluateCoreAsync(Guid childId, Guid unitId)
    {
        var cup = await learningRepository.GetChildUnitProgressAsync(childId, unitId);
        if (cup is null || cup.Status != UnitProgressStatus.Completed)
            return;

        var child = await childRepository.GetById(childId);
        if (child is null)
            return;

        var unit = await curriculumRepository.GetUnit(unitId);
        if (unit is null || unit.ProgramId != child.CurrentProgramId)
        {
            learningRepository.RemoveChildUnitProgress(cup);
            await unitOfWork.SaveChanges();
            return;
        }

        var currentProgram = await curriculumRepository.GetProgram(child.CurrentProgramId);
        if (currentProgram is null)
            return;

        var errors = cup.ErrorCount;
        var currentTrack = currentProgram.DifficultyTrack;
        var newTrack = DecideTargetTrack(currentTrack, errors);

        if (newTrack is null)
        {
            learningRepository.RemoveChildUnitProgress(cup);
            await unitOfWork.SaveChanges();
            return;
        }

        var newProgram = await curriculumRepository.GetProgramByTrack(newTrack.Value);
        if (newProgram is null || !newProgram.IsPublished)
        {
            learningRepository.RemoveChildUnitProgress(cup);
            await unitOfWork.SaveChanges();
            return;
        }

        if (newProgram.Id == child.CurrentProgramId)
        {
            learningRepository.RemoveChildUnitProgress(cup);
            await unitOfWork.SaveChanges();
            return;
        }

        child.CurrentProgramId = newProgram.Id;
        await learningRepository.ClearChildLearningProgressAsync(child.Id);

        var title = "Learning track updated";
        var body = $"{child.DisplayName}'s program was adjusted to {newProgram.Title} based on mistakes in the last completed unit ({errors} incorrect attempts before first correct answers).";

        var notification = new Notification
        {
            ParentId = child.ParentId,
            ChildId = child.Id,
            Type = NotificationType.AdaptiveProgramChange,
            Title = title,
            Body = body
        };
        await notificationRepository.Add(notification);
        await unitOfWork.SaveChanges();

        await hubContext.Clients.Group(child.ParentId.ToString("D")).SendAsync("notification", new
        {
            notification.Id,
            notification.Type,
            notification.Title,
            notification.Body,
            notification.ChildId,
            notification.CreatedAt
        });
    }

    private ProgramDifficultyTrack? DecideTargetTrack(ProgramDifficultyTrack current, int errors)
    {
        if (errors >= Options.DowngradeThreshold && current > ProgramDifficultyTrack.Elementary)
            return (ProgramDifficultyTrack)((int)current - 1);

        if (errors <= Options.UpgradeMax && current < ProgramDifficultyTrack.Intermediate)
            return (ProgramDifficultyTrack)((int)current + 1);

        return null;
    }
}
