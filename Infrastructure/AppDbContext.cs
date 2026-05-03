using LearningPlatform.Domain;
using Microsoft.EntityFrameworkCore;

namespace LearningPlatform.Infrastructure;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Child> Children => Set<Child>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<Exercise> Exercises => Set<Exercise>();
    public DbSet<ChildLessonProgress> ChildLessonProgresses => Set<ChildLessonProgress>();
    public DbSet<ExerciseResult> ExerciseResults => Set<ExerciseResult>();
    public DbSet<Badge> Badges => Set<Badge>();
    public DbSet<ChildBadge> ChildBadges => Set<ChildBadge>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasIndex(x => x.Email).IsUnique();
        modelBuilder.Entity<Child>().HasIndex(x => x.ParentId);
        modelBuilder.Entity<Lesson>().HasIndex(x => new { x.UnitId, x.OrderIndex });
        modelBuilder.Entity<Exercise>().HasIndex(x => new { x.LessonId, x.OrderIndex });
        modelBuilder.Entity<ChildLessonProgress>().HasIndex(x => new { x.ChildId, x.LessonId }).IsUnique();
        modelBuilder.Entity<ExerciseResult>().HasIndex(x => x.ChildId);
        modelBuilder.Entity<Notification>().HasIndex(x => new { x.ParentId, x.IsRead });
        modelBuilder.Entity<ActivityLog>().HasIndex(x => x.CreatedAt);
        modelBuilder.Entity<RefreshToken>().HasIndex(x => x.UserId);
        modelBuilder.Entity<Badge>().HasIndex(x => x.Key).IsUnique();
    }
}
