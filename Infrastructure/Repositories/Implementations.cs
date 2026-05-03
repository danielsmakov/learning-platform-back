using LearningPlatform.Application;
using LearningPlatform.Domain;
using Microsoft.EntityFrameworkCore;

namespace LearningPlatform.Infrastructure.Repositories;

public static class RepoPaging
{
    public static async Task<PagedResponse<T>> ToPagedResponse<T>(this IQueryable<T> query, QueryOptions options)
    {
        var page = Math.Max(options.Page, 1);
        var pageSize = Math.Clamp(options.PageSize, 1, 100);
        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return new PagedResponse<T> { Items = items, Total = total, Page = page, PageSize = pageSize, TotalPages = (int)Math.Ceiling(total / (double)pageSize) };
    }
}

public class UnitOfWork(AppDbContext db) : IUnitOfWork
{
    public Task SaveChanges() => db.SaveChangesAsync();
}

public class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<bool> EmailExists(string email) => db.Users.AnyAsync(x => x.Email == email);
    public Task<User?> GetByEmail(string email) => db.Users.FirstOrDefaultAsync(x => x.Email == email);
    public Task<User?> GetById(Guid id) => db.Users.FirstOrDefaultAsync(x => x.Id == id);
    public Task Add(User user) => db.Users.AddAsync(user).AsTask();
    public Task<int> CountParents() => db.Users.CountAsync(x => x.Role == UserRole.Parent);
}

public class ChildRepository(AppDbContext db) : IChildRepository
{
    public Task<Child?> GetById(Guid id) =>
        db.Children.Include(x => x.CurrentProgram).FirstOrDefaultAsync(x => x.Id == id);
    public Task Add(Child child) => db.Children.AddAsync(child).AsTask();
    public Task Delete(Child child)
    {
        db.Children.Remove(child);
        return Task.CompletedTask;
    }

    public Task<bool> IsOwner(Guid parentId, Guid childId) => db.Children.AnyAsync(x => x.Id == childId && x.ParentId == parentId);
    public Task<PagedResponse<Child>> GetByParent(Guid parentId, QueryOptions query) =>
        db.Children.Include(x => x.CurrentProgram).Where(x => x.ParentId == parentId).OrderBy(x => x.Name).ToPagedResponse(query);
    public Task<List<Guid>> GetAllChildIdsAsync() => db.Children.Select(x => x.Id).ToListAsync();
    public Task<int> CountAll() => db.Children.CountAsync();
    public Task<List<Guid>> DistinctParentIds() => db.Children.Select(x => x.ParentId).Distinct().ToListAsync();
    public Task<List<Child>> GetNotActiveToday(DateOnly today) => db.Children.Where(c => c.LastActivityDate != today).ToListAsync();
    public async Task<PagedResponse<object>> GetLeaderboard(LeaderboardQueryOptions query)
    {
        var q = db.Children.Where(x => x.Age >= query.MinAge && x.Age <= query.MaxAge)
            .OrderByDescending(x => x.XpTotal)
            .Select(x => new { x.DisplayName, x.Age, x.XpTotal, x.CurrentLevel });
        var r = await q.ToPagedResponse(query);
        return new PagedResponse<object> { Items = r.Items.Cast<object>().ToList(), Total = r.Total, Page = r.Page, PageSize = r.PageSize, TotalPages = r.TotalPages };
    }
}

public class CurriculumRepository(AppDbContext db) : ICurriculumRepository
{
    public Task<PagedResponse<LearningProgram>> GetPrograms(QueryOptions query, bool includeUnpublished)
    {
        var q = db.Programs.AsNoTracking().AsQueryable();
        if (!includeUnpublished) q = q.Where(x => x.IsPublished);
        return q.OrderBy(x => x.DifficultyTrack).ToPagedResponse(query);
    }

    public Task<LearningProgram?> GetProgram(Guid id) => db.Programs.FirstOrDefaultAsync(x => x.Id == id);
    public Task<LearningProgram?> GetProgramByTrack(ProgramDifficultyTrack track) => db.Programs.FirstOrDefaultAsync(x => x.DifficultyTrack == track);

    public Task AddProgram(LearningProgram program) => db.Programs.AddAsync(program).AsTask();

    public Task DeleteProgram(LearningProgram program)
    {
        db.Programs.Remove(program);
        return Task.CompletedTask;
    }

    public Task<int> CountUnitsForProgram(Guid programId) => db.Units.CountAsync(x => x.ProgramId == programId);

    public Task<int> CountChildrenUsingProgram(Guid programId) => db.Children.CountAsync(x => x.CurrentProgramId == programId);

    public Task<PagedResponse<Unit>> GetUnits(UnitQueryOptions query, bool restrictToPublishedCatalog = true)
    {
        var q = db.Units.AsNoTracking().AsQueryable();
        if (query.ProgramId.HasValue) q = q.Where(x => x.ProgramId == query.ProgramId.Value);
        if (restrictToPublishedCatalog) q = q.Where(x => x.IsPublished);
        return q.OrderBy(x => x.OrderIndex).ToPagedResponse(query);
    }

    public Task<Unit?> GetUnit(Guid id) => db.Units.FirstOrDefaultAsync(x => x.Id == id);
    public Task AddUnit(Unit unit) => db.Units.AddAsync(unit).AsTask();
    public Task DeleteUnit(Unit unit) { db.Units.Remove(unit); return Task.CompletedTask; }
    public Task<PagedResponse<Lesson>> GetLessons(LessonQueryOptions query, bool restrictToPublishedCatalog = true)
    {
        var q = db.Lessons.AsNoTracking().Include(x => x.Unit).AsQueryable();
        if (query.ProgramId.HasValue) q = q.Where(x => x.Unit!.ProgramId == query.ProgramId.Value);
        if (query.UnitId.HasValue) q = q.Where(x => x.UnitId == query.UnitId.Value);
        if (query.LessonType.HasValue) q = q.Where(x => x.LessonType == query.LessonType.Value);
        if (query.Difficulty.HasValue) q = q.Where(x => x.Difficulty == query.Difficulty.Value);
        if (restrictToPublishedCatalog)
            q = q.Where(x => x.IsPublished && x.Unit!.IsPublished);
        else if (query.IsPublished.HasValue)
            q = q.Where(x => x.IsPublished == query.IsPublished.Value);
        return q.OrderBy(x => x.OrderIndex).ToPagedResponse(query);
    }

    public Task<int> CountPublishedLessonsInUnit(Guid unitId) =>
        db.Lessons.CountAsync(l => l.UnitId == unitId && l.IsPublished);

    public Task<Lesson?> GetLesson(Guid id) =>
        db.Lessons.Include(x => x.Unit).FirstOrDefaultAsync(x => x.Id == id);
    public Task AddLesson(Lesson lesson) => db.Lessons.AddAsync(lesson).AsTask();
    public Task DeleteLesson(Lesson lesson) { db.Lessons.Remove(lesson); return Task.CompletedTask; }
    public Task<PagedResponse<Exercise>> GetExercises(Guid lessonId, QueryOptions query, bool restrictToPublishedCatalog = true)
    {
        var q = db.Exercises.AsNoTracking()
            .Include(x => x.Lesson)
            .ThenInclude(l => l!.Unit)
            .Where(x => x.LessonId == lessonId);
        if (restrictToPublishedCatalog)
            q = q.Where(x => x.Lesson != null && x.Lesson.IsPublished && x.Lesson.Unit != null && x.Lesson.Unit.IsPublished);
        return q.OrderBy(x => x.OrderIndex).ToPagedResponse(query);
    }
    public Task<Exercise?> GetExercise(Guid id) =>
        db.Exercises.Include(x => x.Lesson).ThenInclude(l => l!.Unit).FirstOrDefaultAsync(x => x.Id == id);
    public Task AddExercise(Exercise exercise) => db.Exercises.AddAsync(exercise).AsTask();
    public Task DeleteExercise(Exercise exercise) { db.Exercises.Remove(exercise); return Task.CompletedTask; }
}

public class LearningRepository(AppDbContext db) : ILearningRepository
{
    public Task AddExerciseResult(ExerciseResult result) => db.ExerciseResults.AddAsync(result).AsTask();
    public Task<int> CountExercises(Guid lessonId) => db.Exercises.CountAsync(x => x.LessonId == lessonId);
    public Task<List<Guid>> GetLessonExerciseIds(Guid lessonId) =>
        db.Exercises.Where(x => x.LessonId == lessonId).OrderBy(x => x.OrderIndex).Select(x => x.Id).ToListAsync();

    public Task<bool> HasCorrectAnswerAsync(Guid childId, Guid exerciseId) =>
        db.ExerciseResults.AnyAsync(r => r.ChildId == childId && r.ExerciseId == exerciseId && r.IsCorrect);

    public async Task<bool> AllLessonExercisesHaveCorrectAnswerAsync(Guid childId, Guid lessonId)
    {
        var ids = await GetLessonExerciseIds(lessonId);
        if (ids.Count == 0) return false;
        foreach (var exId in ids)
        {
            if (!await HasCorrectAnswerAsync(childId, exId)) return false;
        }

        return true;
    }

    public Task<int> CountCompletedPublishedLessonsInUnitAsync(Guid childId, Guid unitId) =>
        db.ChildLessonProgresses.CountAsync(p =>
            p.ChildId == childId &&
            p.Status == LessonProgressStatus.Completed &&
            db.Lessons.Any(l => l.Id == p.LessonId && l.UnitId == unitId && l.IsPublished));

    public Task<int> CountDistinctSubmitted(Guid childId, List<Guid> exerciseIds) => db.ExerciseResults.Where(x => x.ChildId == childId && exerciseIds.Contains(x.ExerciseId)).Select(x => x.ExerciseId).Distinct().CountAsync();
    public Task<ChildLessonProgress?> GetProgress(Guid childId, Guid lessonId) => db.ChildLessonProgresses.FirstOrDefaultAsync(x => x.ChildId == childId && x.LessonId == lessonId);
    public Task AddProgress(ChildLessonProgress progress) => db.ChildLessonProgresses.AddAsync(progress).AsTask();

    public async Task<ChildUnitProgress> GetOrCreateChildUnitProgress(Guid childId, Guid unitId)
    {
        var existing = await db.ChildUnitProgresses.FirstOrDefaultAsync(x => x.ChildId == childId && x.UnitId == unitId);
        if (existing is not null)
            return existing;

        var row = new ChildUnitProgress
        {
            ChildId = childId,
            UnitId = unitId,
            Status = UnitProgressStatus.InProgress,
            StartedAt = DateTime.UtcNow
        };
        await db.ChildUnitProgresses.AddAsync(row);
        return row;
    }

    public Task<ChildUnitProgress?> GetChildUnitProgressAsync(Guid childId, Guid unitId) =>
        db.ChildUnitProgresses.FirstOrDefaultAsync(x => x.ChildId == childId && x.UnitId == unitId);

    public void RemoveChildUnitProgress(ChildUnitProgress row)
    {
        db.ChildUnitProgresses.Remove(row);
    }

    public Task<List<ChildUnitProgress>> GetCompletedChildUnitProgressRowsAsync() =>
        db.ChildUnitProgresses.Where(x => x.Status == UnitProgressStatus.Completed).ToListAsync();

    public async Task ClearChildLearningProgressAsync(Guid childId)
    {
        await db.ExerciseResults.Where(x => x.ChildId == childId).ExecuteDeleteAsync();
        await db.ChildLessonProgresses.Where(x => x.ChildId == childId).ExecuteDeleteAsync();
        await db.ChildUnitProgresses.Where(x => x.ChildId == childId).ExecuteDeleteAsync();
    }

    public Task<PagedResponse<ChildLessonProgress>> GetProgressByChild(Guid childId, QueryOptions query) => db.ChildLessonProgresses.Where(x => x.ChildId == childId).OrderByDescending(x => x.CompletedAt).ToPagedResponse(query);
    public Task<int> CountCompletedLessons(Guid childId) => db.ChildLessonProgresses.CountAsync(x => x.ChildId == childId && x.Status == LessonProgressStatus.Completed);
    public Task<int> CountCompletedLessonsAll() => db.ChildLessonProgresses.CountAsync(x => x.Status == LessonProgressStatus.Completed);
    public Task<int> CountProgressRows() => db.ChildLessonProgresses.CountAsync();

    public async Task<Dictionary<Guid, ChildLessonProgress>> GetLessonProgressMapAsync(Guid childId, IReadOnlyCollection<Guid> lessonIds)
    {
        if (lessonIds.Count == 0)
            return new Dictionary<Guid, ChildLessonProgress>();
        return await db.ChildLessonProgresses.AsNoTracking()
            .Where(x => x.ChildId == childId && lessonIds.Contains(x.LessonId))
            .ToDictionaryAsync(x => x.LessonId);
    }
}

public class ContentTranslationRepository(AppDbContext db) : IContentTranslationRepository
{
    public Task<List<ContentTranslation>> ListAsync(string entityType, IReadOnlyCollection<Guid> entityIds)
    {
        if (entityIds.Count == 0)
            return Task.FromResult(new List<ContentTranslation>());

        return db.ContentTranslations.AsNoTracking()
            .Where(x => x.EntityType == entityType && entityIds.Contains(x.EntityId))
            .ToListAsync();
    }
}

public class BadgeRepository(AppDbContext db) : IBadgeRepository
{
    public Task<List<Badge>> GetAll() => db.Badges.ToListAsync();
    public Task<bool> Any() => db.Badges.AnyAsync();
    public Task<Badge?> GetByKey(string key) => db.Badges.FirstOrDefaultAsync(x => x.Key == key);
    public Task Add(Badge badge) => db.Badges.AddAsync(badge).AsTask();
    public Task AddRange(params Badge[] badges) => db.Badges.AddRangeAsync(badges);
    public Task<List<Guid>> GetEarnedBadgeIds(Guid childId) => db.ChildBadges.Where(x => x.ChildId == childId).Select(x => x.BadgeId).ToListAsync();
    public Task AddChildBadge(ChildBadge childBadge) => db.ChildBadges.AddAsync(childBadge).AsTask();
    public async Task<PagedResponse<object>> GetChildBadges(Guid childId, QueryOptions query)
    {
        var q = from cb in db.ChildBadges
                join b in db.Badges on cb.BadgeId equals b.Id
                where cb.ChildId == childId
                select new { b.Key, b.Name, cb.AwardedAt };
        var p = await q.ToPagedResponse(query);
        return new PagedResponse<object> { Items = p.Items.Cast<object>().ToList(), Total = p.Total, Page = p.Page, PageSize = p.PageSize, TotalPages = p.TotalPages };
    }
}

public class NotificationRepository(AppDbContext db) : INotificationRepository
{
    public Task Add(Notification notification) => db.Notifications.AddAsync(notification).AsTask();
    public Task<PagedResponse<Notification>> GetByParent(Guid parentId, QueryOptions query) => db.Notifications.Where(x => x.ParentId == parentId).OrderByDescending(x => x.CreatedAt).ToPagedResponse(query);
    public Task<List<Notification>> GetByParentAndIds(Guid parentId, IReadOnlyCollection<Guid> ids) => db.Notifications.Where(x => x.ParentId == parentId && ids.Contains(x.Id)).ToListAsync();
}

public class AuthRepository(AppDbContext db) : IAuthRepository
{
    public Task AddRefreshToken(RefreshToken token) => db.RefreshTokens.AddAsync(token).AsTask();

    public Task<RefreshToken?> FindValidRefreshToken(string rawToken)
    {
        var token = db.RefreshTokens.Where(x => !x.IsRevoked && x.ExpiresAt > DateTime.UtcNow).AsEnumerable().FirstOrDefault(x => BCrypt.Net.BCrypt.Verify(rawToken, x.TokenHash));
        return Task.FromResult(token);
    }

    public Task<RefreshToken?> FindActiveRefreshToken(string rawToken)
    {
        var token = db.RefreshTokens.Where(x => !x.IsRevoked).AsEnumerable().FirstOrDefault(x => BCrypt.Net.BCrypt.Verify(rawToken, x.TokenHash));
        return Task.FromResult(token);
    }
}

public class ActivityLogRepository(AppDbContext db) : IActivityLogRepository
{
    public Task Add(ActivityLog log) => db.ActivityLogs.AddAsync(log).AsTask();
    public async Task<PagedResponse<object>> GetLogs(QueryOptions query)
    {
        var q = db.ActivityLogs.OrderByDescending(x => x.CreatedAt)
            .Select(x => new { x.AdminId, x.Action, x.ResourceType, x.ResourceId, x.CreatedAt });
        var p = await q.ToPagedResponse(query);
        return new PagedResponse<object> { Items = p.Items.Cast<object>().ToList(), Total = p.Total, Page = p.Page, PageSize = p.PageSize, TotalPages = p.TotalPages };
    }
}
