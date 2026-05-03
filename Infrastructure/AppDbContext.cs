using LearningPlatform.Domain;
using Microsoft.EntityFrameworkCore;

namespace LearningPlatform.Infrastructure;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Child> Children => Set<Child>();
    public DbSet<LearningProgram> Programs => Set<LearningProgram>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<Exercise> Exercises => Set<Exercise>();
    public DbSet<ChildLessonProgress> ChildLessonProgresses => Set<ChildLessonProgress>();
    public DbSet<ChildUnitProgress> ChildUnitProgresses => Set<ChildUnitProgress>();
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
        modelBuilder.Entity<Child>().HasIndex(x => x.CurrentProgramId);
        modelBuilder.Entity<LearningProgram>().HasIndex(x => x.DifficultyTrack).IsUnique();
        modelBuilder.Entity<LearningProgram>().HasIndex(x => x.IsDefault).IsUnique().HasFilter("\"IsDefault\" = TRUE");
        modelBuilder.Entity<Unit>().HasIndex(x => x.ProgramId);
        modelBuilder.Entity<Child>().HasOne(x => x.CurrentProgram).WithMany().HasForeignKey(x => x.CurrentProgramId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Unit>().HasOne(x => x.Program).WithMany(x => x.Units).HasForeignKey(x => x.ProgramId).OnDelete(DeleteBehavior.Restrict);
        // Match InitialCreate: deleting a unit/lesson cascades to children in the D2 tree.
        modelBuilder.Entity<Lesson>().HasOne(x => x.Unit).WithMany(x => x.Lessons).HasForeignKey(x => x.UnitId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Exercise>().HasOne(x => x.Lesson).WithMany(x => x.Exercises).HasForeignKey(x => x.LessonId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Lesson>().HasIndex(x => new { x.UnitId, x.OrderIndex });
        modelBuilder.Entity<Exercise>().HasIndex(x => new { x.LessonId, x.OrderIndex });
        modelBuilder.Entity<ChildLessonProgress>().HasIndex(x => new { x.ChildId, x.LessonId }).IsUnique();
        modelBuilder.Entity<ChildUnitProgress>().HasIndex(x => new { x.ChildId, x.UnitId }).IsUnique();
        modelBuilder.Entity<ChildUnitProgress>().HasOne(x => x.Child).WithMany().HasForeignKey(x => x.ChildId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ChildUnitProgress>().HasOne(x => x.Unit).WithMany().HasForeignKey(x => x.UnitId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ExerciseResult>().HasIndex(x => x.ChildId);
        modelBuilder.Entity<Notification>().HasIndex(x => new { x.ParentId, x.IsRead });
        modelBuilder.Entity<ActivityLog>().HasIndex(x => x.CreatedAt);
        modelBuilder.Entity<RefreshToken>().HasIndex(x => x.UserId);
        modelBuilder.Entity<Badge>().HasIndex(x => x.Key).IsUnique();
    }
}
