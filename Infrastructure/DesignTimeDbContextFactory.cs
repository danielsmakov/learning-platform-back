using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace LearningPlatform.Infrastructure;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var projectDir = Directory.GetCurrentDirectory();
        var envPath = Path.Combine(projectDir, ".env");
        if (File.Exists(envPath))
            Env.Load(envPath);

        var configuration = new ConfigurationBuilder()
            .SetBasePath(projectDir)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();

        var cs = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(cs))
        {
            throw new InvalidOperationException(
                "Database connection string not found. Add ConnectionStrings__DefaultConnection to .env (see .env.example).");
        }

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(cs)
            .Options;

        return new AppDbContext(options);
    }
}
