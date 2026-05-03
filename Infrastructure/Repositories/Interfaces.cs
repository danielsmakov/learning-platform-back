using LearningPlatform.Application;
using LearningPlatform.Domain;

namespace LearningPlatform.Infrastructure.Repositories;

public interface IUserRepository
{
    Task<bool> EmailExists(string email);
    Task<User?> GetByEmail(string email);
    Task<User?> GetById(Guid id);
    Task Add(User user);
    Task<int> CountParents();
}

public interface IChildRepository
{
    Task<Child?> GetById(Guid id);
    Task Add(Child child);
    Task Delete(Child child);
    Task<bool> IsOwner(Guid parentId, Guid childId);
    Task<PagedResponse<Child>> GetByParent(Guid parentId, QueryOptions query);
    Task<List<Guid>> GetAllChildIdsAsync();
    Task<int> CountAll();
    Task<List<Guid>> DistinctParentIds();
    Task<List<Child>> GetNotActiveToday(DateOnly today);
    /// <summary>P3 / C5: все дети родителя (для еженедельного агрегата).</summary>
    Task<List<Child>> ListChildrenForParentAsync(Guid parentId);
    Task<PagedResponse<object>> GetLeaderboard(LeaderboardQueryOptions query);
}

public interface ICurriculumRepository
{
    Task<PagedResponse<LearningProgram>> GetPrograms(QueryOptions query, bool includeUnpublished);
    Task<LearningProgram?> GetProgram(Guid id);
    Task<LearningProgram?> GetProgramByTrack(ProgramDifficultyTrack track);
    Task AddProgram(LearningProgram program);
    Task DeleteProgram(LearningProgram program);
    Task<int> CountUnitsForProgram(Guid programId);
    Task<int> CountChildrenUsingProgram(Guid programId);

    Task<PagedResponse<Unit>> GetUnits(UnitQueryOptions query, bool restrictToPublishedCatalog = true);
    Task<Unit?> GetUnit(Guid id);
    Task AddUnit(Unit unit);
    Task DeleteUnit(Unit unit);
    Task<PagedResponse<Lesson>> GetLessons(LessonQueryOptions query, bool restrictToPublishedCatalog = true);
    Task<int> CountPublishedLessonsInUnit(Guid unitId);
    Task<Lesson?> GetLesson(Guid id);
    Task AddLesson(Lesson lesson);
    Task DeleteLesson(Lesson lesson);
    Task<PagedResponse<Exercise>> GetExercises(Guid lessonId, QueryOptions query, bool restrictToPublishedCatalog = true);
    Task<Exercise?> GetExercise(Guid id);
    Task AddExercise(Exercise exercise);
    Task DeleteExercise(Exercise exercise);
}

public interface ILearningRepository
{
    Task AddExerciseResult(ExerciseResult result);
    Task<int> CountExercises(Guid lessonId);
    Task<List<Guid>> GetLessonExerciseIds(Guid lessonId);
    /// <summary>Есть ли хотя бы один верный ответ по паре ребёнок / упражнение.</summary>
    Task<bool> HasCorrectAnswerAsync(Guid childId, Guid exerciseId);
    Task<bool> AllLessonExercisesHaveCorrectAnswerAsync(Guid childId, Guid lessonId);
    /// <summary>Последний по времени результат по любому упражнению урока (для идемпотентного submit).</summary>
    Task<ExerciseResult?> GetLatestExerciseResultForLessonAsync(Guid childId, Guid lessonId);
    Task<int> CountCompletedPublishedLessonsInUnitAsync(Guid childId, Guid unitId);
    Task<int> CountDistinctSubmitted(Guid childId, List<Guid> exerciseIds);
    Task<ChildLessonProgress?> GetProgress(Guid childId, Guid lessonId);
    Task AddProgress(ChildLessonProgress progress);
    /// <summary>Создаёт строку прогресса по юниту при первом событии (ошибка или завершение юнита), если ещё нет.</summary>
    Task<ChildUnitProgress> GetOrCreateChildUnitProgress(Guid childId, Guid unitId);
    Task<ChildUnitProgress?> GetChildUnitProgressAsync(Guid childId, Guid unitId);
    void RemoveChildUnitProgress(ChildUnitProgress row);
    Task<List<ChildUnitProgress>> GetCompletedChildUnitProgressRowsAsync();
    /// <summary>G3: сброс прохождения контента при смене программы (уроки, юниты, попытки).</summary>
    Task ClearChildLearningProgressAsync(Guid childId);
    Task<PagedResponse<ChildLessonProgress>> GetProgressByChild(Guid childId, QueryOptions query);
    Task<int> CountCompletedLessons(Guid childId);
    Task<int> CountCompletedLessonsAll();
    Task<int> CountProgressRows();
    /// <summary>Прогресс по списку уроков (для карты куррикулума).</summary>
    Task<Dictionary<Guid, ChildLessonProgress>> GetLessonProgressMapAsync(Guid childId, IReadOnlyCollection<Guid> lessonIds);

    /// <summary>P3 / C5: агрегаты из <see cref="ExerciseResult"/>, <see cref="ChildLessonProgress"/>, <see cref="ChildUnitProgress"/> за интервал UTC.</summary>
    Task<WeeklyActivityStats> GetWeeklyActivityStatsAsync(Guid childId, DateTime startUtcInclusive, DateTime endUtcExclusive);
}

public interface IContentTranslationRepository
{
    Task<List<ContentTranslation>> ListAsync(string entityType, IReadOnlyCollection<Guid> entityIds);
}

public interface IBadgeRepository
{
    Task<List<Badge>> GetAll();
    Task<bool> Any();
    Task<Badge?> GetByKey(string key);
    Task Add(Badge badge);
    Task AddRange(params Badge[] badges);
    Task<List<Guid>> GetEarnedBadgeIds(Guid childId);
    Task AddChildBadge(ChildBadge childBadge);
    Task<PagedResponse<object>> GetChildBadges(Guid childId, QueryOptions query);
}

/// <summary>P3 / C1: каждое родительское уведомление сначала INSERT в таблицу Notifications, затем SaveChanges, затем SignalR.</summary>
public interface INotificationRepository
{
    Task Add(Notification notification);
    Task<PagedResponse<Notification>> GetByParent(Guid parentId, QueryOptions query);
    Task<List<Notification>> GetByParentAndIds(Guid parentId, IReadOnlyCollection<Guid> ids);
}

public interface IAuthRepository
{
    Task AddRefreshToken(RefreshToken token);
    Task<RefreshToken?> FindValidRefreshToken(string rawToken);
    Task<RefreshToken?> FindActiveRefreshToken(string rawToken);
}

public interface IActivityLogRepository
{
    Task Add(ActivityLog log);
    Task<PagedResponse<object>> GetLogs(QueryOptions query);
}

public interface IUnitOfWork
{
    Task SaveChanges();
}
