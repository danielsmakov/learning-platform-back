using LearningPlatform.Domain;

namespace LearningPlatform.Application;

public record RegisterRequest(string Email, string Password);
public record LoginRequest(string Email, string Password);
public record ChildLoginRequest(Guid ChildId, string Pin);
public record RefreshRequest(string RefreshToken);
public record AuthResponse(Guid UserId, string Email, string Role, string AccessToken, string RefreshToken, DateTime ExpiresAt);

public record UpdateParentRequest(string Email);
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
public record CreateLessonRequest(Guid UnitId, string Title, int OrderIndex, LessonType LessonType, Difficulty Difficulty, int XpReward, bool IsPublished);
public record UpdateLessonRequest(string Title, int OrderIndex, LessonType LessonType, Difficulty Difficulty, int XpReward, bool IsPublished);
public record CreateExerciseRequest(LessonType ExerciseType, int OrderIndex, string Content);
public record UpdateExerciseRequest(LessonType ExerciseType, int OrderIndex, string Content);

public record SubmitExerciseRequest(Guid ChildId, bool IsCorrect, int TimeTakenMs);
public record CompleteLessonRequest(Guid ChildId);
public record LessonResumeResponse(Guid? NextExerciseId, bool IsLessonCompleted);
public record ExerciseSubmitResponse(Guid ResultId, bool IsCorrect, int TimeTakenMs, DateTime SubmittedAt, bool LessonJustCompleted);
public record MarkNotificationsReadRequest(List<Guid> NotificationIds);

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
}

public class LeaderboardQueryOptions : QueryOptions
{
    public int MinAge { get; set; } = 5;
    public int MaxAge { get; set; } = 12;
}

public class PagedResponse<T>
{
    public IReadOnlyCollection<T> Items { get; init; } = [];
    public int Total { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
}
