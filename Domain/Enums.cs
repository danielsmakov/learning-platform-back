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

public enum NotificationType
{
    Milestone = 1,
    StreakReminder = 2,
    WeeklySummary = 3,
    AdaptiveProgramChange = 4
}

/// <summary>Дорожка уровня учебной программы (не путать с <see cref="Difficulty"/> урока).</summary>
public enum ProgramDifficultyTrack
{
    Elementary = 1,
    Beginner = 2,
    PreIntermediate = 3,
    Intermediate = 4
}
