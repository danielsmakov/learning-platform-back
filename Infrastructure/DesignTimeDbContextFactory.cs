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
        // Достаточно для scaffolding миграций (dotnet ef migrations add); реальный запуск приложения по-прежнему требует .env.
        if (string.IsNullOrWhiteSpace(cs))
            cs = "Host=127.0.0.1;Port=5432;Database=ef_design_dummy;Username=dummy;Password=dummy";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(cs)
            .Options;

        return new AppDbContext(options);
    }
}
