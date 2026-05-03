namespace LearningPlatform.Domain;

public enum UserRole
{
    Parent = 1,
    Admin = 2,
    Child = 3
}

public enum LessonType
{
    Phonics = 1,
    Handwriting = 2,
    SightWords = 3,
    Vocabulary = 4
}

public enum Difficulty
{
    Easy = 1,
    Medium = 2,
    Hard = 3
}

public enum LessonProgressStatus
{
    NotStarted = 1,
    InProgress = 2,
    Completed = 3
}

/// <summary>Прохождение юнита ребёнком (E1); не путать со справочником <see cref="Unit"/>.</summary>
public enum UnitProgressStatus
{
    NotStarted = 1,
    InProgress = 2,
    Completed = 3
}

/// <summary>
/// G4: типы родительских уведомлений (БД + при онлайн — SignalR). Пункты ТЗ: (1)=4, (2)=5, (3)=6, (4)=7, (5)=2;
/// также Milestone (урок), WeeklySummary (резюме).
/// </summary>
public enum NotificationType
{
    /// <summary>Завершение урока (милестоун); payload как у остальных G4.</summary>
    Milestone = 1,
    /// <summary>G4 (5): streak at-risk, Hangfire 18:00 по локальному времени сервера.</summary>
    StreakReminder = 2,
    WeeklySummary = 3,
    /// <summary>G4 (1): смена программы / adaptive track.</summary>
    AdaptiveProgramChange = 4,
    /// <summary>G4 (2): бейдж / ачивка.</summary>
    BadgeEarned = 5,
    /// <summary>G4 (3): повышение уровня (XP / геймификация).</summary>
    LevelUp = 6,
    /// <summary>G4 (4): успешное завершение юнита.</summary>
    UnitCompleted = 7
}

/// <summary>Дорожка уровня учебной программы (не путать с <see cref="Difficulty"/> урока).</summary>
public enum ProgramDifficultyTrack
{
    Elementary = 1,
    Beginner = 2,
    PreIntermediate = 3,
    Intermediate = 4
}
