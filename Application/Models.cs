using LearningPlatform.Domain;

namespace LearningPlatform.Application;

public record RegisterRequest(string Email, string Password);
public record LoginRequest(string Email, string Password);
public record ChildLoginRequest(Guid ChildId, string Pin);
public record RefreshRequest(string RefreshToken);
public record AuthResponse(Guid UserId, string Email, string Role, string AccessToken, string RefreshToken, DateTime ExpiresAt);

public record UpdateParentRequest(string Email);
public record ChangeParentPasswordRequest(string CurrentPassword, string NewPassword);
public record CreateChildRequest(
    Guid ParentId,
    string Name,
    int Age,
    string AvatarUrl,
    string DisplayName,
    string Pin,
    ProgramDifficultyTrack LearningProgramTrack = ProgramDifficultyTrack.Beginner);
public record UpdateChildRequest(
    string Name,
    int Age,
    string AvatarUrl,
    string DisplayName,
    ProgramDifficultyTrack? LearningProgramTrack = null);

/// <summary>
/// Данные ребёнка для API (без секретов); включает дорожку программы.
/// <para><b>CurrentUnitProgressPercent</b> — доля завершённых уроков в «текущем» юните:
/// число уроков со статусом Completed среди опубликованных уроков этого юнита, делённое на число всех опубликованных уроков юнита, ×100, округление до целого (0–100).
/// Текущий юнит — первый по порядку (<c>OrderIndex</c>) опубликованный юнит программы ребёнка, в котором есть хотя бы один незавершённый опубликованный урок;
/// если все такие уроки во всех юнитах завершены — берётся последний юнит программы, где есть опубликованные уроки, процент 100.
/// В программе без опубликованных уроков: 0 и <see cref="CurrentUnitId"/> = null.</para>
/// </summary>
public record ChildResponse(
    Guid Id,
    Guid ParentId,
    string Name,
    int Age,
    string AvatarUrl,
    string DisplayName,
    int CurrentLevel,
    int XpTotal,
    int StreakCurrent,
    int StreakLongest,
    DateOnly? LastActivityDate,
    DateTime CreatedAt,
    Guid CurrentProgramId,
    ProgramDifficultyTrack LearningProgramTrack,
    int CurrentUnitProgressPercent,
    Guid? CurrentUnitId);

public record CreateLearningProgramRequest(ProgramDifficultyTrack DifficultyTrack, string Title, string Description, bool IsPublished);
public record UpdateLearningProgramRequest(string Title, string Description, bool IsPublished);

public record CreateUnitRequest(Guid ProgramId, string Title, string Description, int OrderIndex, bool IsPublished);
public record UpdateUnitRequest(string Title, string Description, int OrderIndex, bool IsPublished, Guid? ProgramId = null);
public record CreateLessonRequest(Guid UnitId, string Title, string Description, int OrderIndex, LessonType LessonType, Difficulty Difficulty, int XpReward, bool IsPublished);
public record UpdateLessonRequest(string Title, string Description, int OrderIndex, LessonType LessonType, Difficulty Difficulty, int XpReward, bool IsPublished);
public record CreateExerciseRequest(LessonType ExerciseType, int OrderIndex, string Content);
public record UpdateExerciseRequest(LessonType ExerciseType, int OrderIndex, string Content);

public record SubmitExerciseRequest(Guid ChildId, bool IsCorrect, int TimeTakenMs);
public record CompleteLessonRequest(Guid ChildId);
public record LessonResumeResponse(Guid? NextExerciseId, bool IsLessonCompleted);
public record ExerciseSubmitResponse(Guid ResultId, bool IsCorrect, int TimeTakenMs, DateTime SubmittedAt, bool LessonJustCompleted);
public record MarkNotificationsReadRequest(List<Guid> NotificationIds);

/// <summary>P3 / C5: агрегаты за окно времени [StartUtcInclusive, EndUtcExclusive) для одного ребёнка.</summary>
public sealed record WeeklyActivityStats(
    int ExerciseSubmissions,
    int IncorrectSubmissions,
    int LessonsCompletedInPeriod,
    int UnitsCompletedInPeriod,
    int DistinctLessonsWithSubmissions,
    int DistinctUnitsTouched);

/// <summary>H1: пагинация списков (в query: <c>page</c>, <c>pageSize</c> — регистронезависимо).</summary>
public class QueryOptions
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class UnitQueryOptions : QueryOptions
{
    /// <summary>Explicit program (anonymous / admin). Admin catalog requires this.</summary>
    public Guid? ProgramId { get; set; }
    /// <summary>Parent: каталог в контексте выбранного ребёнка (D4).</summary>
    public Guid? ChildId { get; set; }
}

public class LessonQueryOptions : QueryOptions
{
    public Guid? UnitId { get; set; }
    /// <summary>Фильтр уроков по программе (после резолва каталога).</summary>
    public Guid? ProgramId { get; set; }
    /// <summary>Parent: резолв программы по ребёнку.</summary>
    public Guid? ChildId { get; set; }
    public LessonType? LessonType { get; set; }
    public Difficulty? Difficulty { get; set; }
    public bool? IsPublished { get; set; }
    /// <summary>H2: подстрока в Title или Description урока (без учёта регистра); только Admin.</summary>
    public string? Search { get; set; }
}

public class LeaderboardQueryOptions : QueryOptions
{
    public int MinAge { get; set; } = 5;
    public int MaxAge { get; set; } = 12;
}

/// <summary>
/// B1 / G1: карта куррикулума ребёнка в текущей программе.
/// <para><see cref="CurrentUnitProgressPercent"/> и <see cref="CurrentUnitId"/> — та же метрика и формула, что в <see cref="ChildResponse"/>.</para>
/// </summary>
public record CurriculumMapResponse(
    Guid ChildId,
    Guid ProgramId,
    ProgramDifficultyTrack LearningProgramTrack,
    string ProgramTitle,
    string ProgramDescription,
    int CurrentUnitProgressPercent,
    Guid? CurrentUnitId,
    IReadOnlyList<CurriculumMapUnitDto> Units,
    CurriculumMapNextDto Next);

public record CurriculumMapUnitDto(
    Guid UnitId,
    string Title,
    string Description,
    int OrderIndex,
    CurriculumMapUnitStatus Status,
    IReadOnlyList<CurriculumMapLessonDto> Lessons);

public record CurriculumMapLessonDto(
    Guid LessonId,
    string Title,
    int OrderIndex,
    CurriculumMapLessonStatus Status);

/// <summary>Юнит заблокирован предыдущими; открыт; все уроки завершены.</summary>
public enum CurriculumMapUnitStatus
{
    Locked = 1,
    Open = 2,
    Completed = 3
}

public enum CurriculumMapLessonStatus
{
    Locked = 1,
    NotStarted = 2,
    InProgress = 3,
    Completed = 4
}

/// <summary>«Что дальше»: следующий урок по порядку в первом незакрытом юните.</summary>
public record CurriculumMapNextDto(
    string Summary,
    Guid? NextLessonId,
    Guid? NextUnitId);
