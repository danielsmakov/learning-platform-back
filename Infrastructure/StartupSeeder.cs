using LearningPlatform.Domain;
using Microsoft.EntityFrameworkCore;

namespace LearningPlatform.Infrastructure;

public class StartupSeeder(AppDbContext db, IConfiguration configuration)
{
    public async Task SeedAsync()
    {
        await db.Database.MigrateAsync();

        var adminEmail = configuration["Seed:AdminEmail"];
        var adminPassword = configuration["Seed:AdminPassword"];
        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
            return;

        var exists = await db.Users.AnyAsync(x => x.Email == adminEmail);
        if (!exists)
        {
            db.Users.Add(new User
            {
                Email = adminEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword, workFactor: 12),
                Role = UserRole.Admin
            });
            await db.SaveChangesAsync();
        }
    }
}
