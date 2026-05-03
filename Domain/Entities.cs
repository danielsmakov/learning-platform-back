using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LearningPlatform.Domain;

public class User
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(255)] public string Email { get; set; } = string.Empty;
    [MaxLength(255)] public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Parent;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Child
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ParentId { get; set; }
    public User? Parent { get; set; }
    [MaxLength(100)] public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    [MaxLength(255)] public string AvatarUrl { get; set; } = string.Empty;
    [MaxLength(64)] public string DisplayName { get; set; } = string.Empty;
    [MaxLength(255)] public string PinHash { get; set; } = string.Empty;
    public int CurrentLevel { get; set; } = 1;
    public int XpTotal { get; set; }
    public int StreakCurrent { get; set; }
    public int StreakLongest { get; set; }
    public DateOnly? LastActivityDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid CurrentProgramId { get; set; }
    public LearningProgram? CurrentProgram { get; set; }
}

/// <summary>
/// Learning track. Content hierarchy (D2):
/// <see cref="LearningProgram"/> → <see cref="Unit"/> → <see cref="Lesson"/> → <see cref="Exercise"/>.
/// </summary>
[Table("Programs")]
public class LearningProgram
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public ProgramDifficultyTrack DifficultyTrack { get; set; }
    [MaxLength(200)] public string Title { get; set; } = string.Empty;
    [MaxLength(2000)] public string Description { get; set; } = string.Empty;
    public bool IsPublished { get; set; } = true;
    /// <summary>Ровно одна программа в каталоге помечена как дорожка по умолчанию (миграция D5).</summary>
    public bool IsDefault { get; set; }
    public ICollection<Unit> Units { get; set; } = [];
}

/// <summary>Catalog unit; belongs to exactly one <see cref="LearningProgram"/>.</summary>
public class Unit
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProgramId { get; set; }
    public LearningProgram? Program { get; set; }
    [MaxLength(200)] public string Title { get; set; } = string.Empty;
    [MaxLength(2000)] public string Description { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
    public bool IsPublished { get; set; }
    public ICollection<Lesson> Lessons { get; set; } = [];
}

/// <summary>Lesson; linked to a <see cref="Unit"/> (program track flows through the unit).</summary>
public class Lesson
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UnitId { get; set; }
    public Unit? Unit { get; set; }
    [MaxLength(200)] public string Title { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
    public LessonType LessonType { get; set; }
    public Difficulty Difficulty { get; set; } = Difficulty.Easy;
    public int XpReward { get; set; } = 10;
    public bool IsPublished { get; set; }
    public ICollection<Exercise> Exercises { get; set; } = [];
}

/// <summary>Exercise; linked to a <see cref="Lesson"/>.</summary>
public class Exercise
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LessonId { get; set; }
    public Lesson? Lesson { get; set; }
    public LessonType ExerciseType { get; set; }
    public int OrderIndex { get; set; }
    [MaxLength(5000)] public string Content { get; set; } = string.Empty;
}

public class ChildLessonProgress
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ChildId { get; set; }
    public Guid LessonId { get; set; }
    public LessonProgressStatus Status { get; set; } = LessonProgressStatus.NotStarted;
    /// <summary>Последнее упражнение урока, по которому получен первый верный ответ (resume внутри урока).</summary>
    public Guid? LastCompletedExerciseId { get; set; }
    public int XpAwarded { get; set; }
    public DateTime? CompletedAt { get; set; }
}

/// <summary>
/// Ошибки и статус прохождения юнита для пары (ребёнок, юнит). Счётчик не хранится в справочнике <see cref="Unit"/> (E1).
/// </summary>
public class ChildUnitProgress
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ChildId { get; set; }
    public Child? Child { get; set; }
    public Guid UnitId { get; set; }
    public Unit? Unit { get; set; }
    public int ErrorCount { get; set; }
    public UnitProgressStatus Status { get; set; } = UnitProgressStatus.NotStarted;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class ExerciseResult
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ChildId { get; set; }
    public Guid ExerciseId { get; set; }
    public bool IsCorrect { get; set; }
    public int TimeTakenMs { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
}

public class Badge
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(80)] public string Key { get; set; } = string.Empty;
    [MaxLength(200)] public string Name { get; set; } = string.Empty;
    [MaxLength(64)] public string ConditionType { get; set; } = string.Empty;
    public int ConditionValue { get; set; }
}

public class ChildBadge
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ChildId { get; set; }
    public Guid BadgeId { get; set; }
    public DateTime AwardedAt { get; set; } = DateTime.UtcNow;
}

public class Notification
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ParentId { get; set; }
    public Guid? ChildId { get; set; }
    public NotificationType Type { get; set; }
    [MaxLength(200)] public string Title { get; set; } = string.Empty;
    [MaxLength(2000)] public string Body { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class ActivityLog
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AdminId { get; set; }
    [MaxLength(80)] public string Action { get; set; } = string.Empty;
    [MaxLength(80)] public string ResourceType { get; set; } = string.Empty;
    [MaxLength(80)] public string ResourceId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class RefreshToken
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    [MaxLength(255)] public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
}
