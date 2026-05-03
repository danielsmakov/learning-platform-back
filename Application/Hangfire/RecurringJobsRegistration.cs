using Hangfire;
using LearningPlatform.Application.Services;

namespace LearningPlatform.Application.Hangfire;

/// <summary>
/// P3 / C3: регистрация фоновых recurring job — еженедельное резюме, массовая проверка бейджей,
/// плановый прогон adaptive по <c>ErrorCount</c> завершённых юнитов (см. E4). Напоминания по стрику — G4.
/// </summary>
public static class RecurringJobsRegistration
{
    /// <summary>Регистрирует все recurring job после сидирования БД.</summary>
    public static void RegisterP3C3Jobs(IRecurringJobManager recurringJobManager)
    {
        // G4: streak at-risk (18:00 server local / Hangfire TZ)
        recurringJobManager.AddOrUpdate<NotificationService>(
            "streak-reminders",
            s => s.CreateDailyStreakReminders(),
            "0 18 * * *");

        // C3: weekly summary → Notifications (тело расширяется в C5)
        recurringJobManager.AddOrUpdate<NotificationService>(
            "weekly-summary",
            s => s.CreateWeeklySummaries(),
            "0 12 * * 0");

        // C3 + E4: adaptive thresholds по error_count для завершённых юнитов (catch-up, если событие пропущено)
        recurringJobManager.AddOrUpdate<AdaptiveDifficultyJob>(
            "adaptive-difficulty-scheduled",
            j => j.ProcessScheduledCompletedUnitsAsync(),
            "*/15 * * * *");

        // C3 + F1: переоценка бейджей по всем детям (чем больше сидов в БД — тем полнее «10–12» условий)
        recurringJobManager.AddOrUpdate<BadgeEvaluationJob>(
            "badge-evaluation-all-children",
            j => j.EvaluateAllChildrenAsync(),
            "0 2 * * *");
    }
}
