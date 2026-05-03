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
    Task<int> CountAll();
    Task<List<Guid>> DistinctParentIds();
    Task<List<Child>> GetNotActiveToday(DateOnly today);
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
    Task<int> CountDistinctSubmitted(Guid childId, List<Guid> exerciseIds);
    Task<ChildLessonProgress?> GetProgress(Guid childId, Guid lessonId);
    Task AddProgress(ChildLessonProgress progress);
    Task<PagedResponse<ChildLessonProgress>> GetProgressByChild(Guid childId, QueryOptions query);
    Task<int> CountCompletedLessons(Guid childId);
    Task<int> CountCompletedLessonsAll();
    Task<int> CountProgressRows();
}

public interface IBadgeRepository
{
    Task<List<Badge>> GetAll();
    Task<bool> Any();
    Task AddRange(params Badge[] badges);
    Task<List<Guid>> GetEarnedBadgeIds(Guid childId);
    Task AddChildBadge(ChildBadge childBadge);
    Task<PagedResponse<object>> GetChildBadges(Guid childId, QueryOptions query);
}

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
