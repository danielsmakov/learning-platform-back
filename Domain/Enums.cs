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

public enum NotificationType
{
    Milestone = 1,
    StreakReminder = 2,
    WeeklySummary = 3
}
