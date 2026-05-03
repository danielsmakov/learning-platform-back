using Hangfire;
using Hangfire.PostgreSql;
using FluentValidation;
using LearningPlatform;
using LearningPlatform.Application;
using LearningPlatform.Application.Services;
using LearningPlatform.Infrastructure;
using LearningPlatform.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.OpenApi;

EnvBootstrap.LoadLocalEnvFile();
var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Learning Platform API",
        Version = "v1",
        Description =
            "**Accept-Language (G2):** optional on catalog GETs (`/api/v1/programs`, `/units`, `/units/{id}`, `/lessons`, `/lessons/{id}`, `/lessons/{id}/exercises`) and `GET .../children/{id}/curriculum-map`. " +
            "Send `ru`, `en`, or full tags like `ru-RU;q=0.9,en;q=0.8` — first tag wins (`ru*` → Russian, else English). " +
            "See each operation’s `Accept-Language` header parameter in OpenAPI."
    });
});
builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();
builder.Services.AddScoped<ValidationActionFilter>();
builder.Services.AddControllers(options =>
{
    options.Filters.AddService<ValidationActionFilter>();
});
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendCors", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5173",
                "http://127.0.0.1:5173",
                "http://localhost:5174",
                "http://127.0.0.1:5174")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(config.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = JwtTokenService.GetValidationParameters(config);
        // H3 / G4: браузерный SignalR — JWT в query `access_token` (см. README фронта).
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var path = context.HttpContext.Request.Path;
                if (!path.StartsWithSegments("/hubs"))
                    return Task.CompletedTask;
                if (!string.IsNullOrEmpty(context.Request.Query["access_token"]))
                    context.Token = context.Request.Query["access_token"];
                else if (context.Request.Headers.TryGetValue("Authorization", out var auth) &&
                         auth.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    context.Token = auth.ToString()["Bearer ".Length..].Trim();
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddSignalR();

builder.Services.AddHangfire(h =>
{
    h.UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UsePostgreSqlStorage(c => c.UseNpgsqlConnection(config.GetConnectionString("DefaultConnection")));
});
builder.Services.AddHangfireServer();

builder.Services.AddOptions<AdaptiveErrorsOptions>()
    .Configure<IConfiguration>((opts, cfg) =>
    {
        cfg.GetSection(AdaptiveErrorsOptions.SectionName).Bind(opts);
        if (int.TryParse(cfg["ADAPTIVE_ERRORS_DOWNGRADE_THRESHOLD"], out var d) && d > 0)
            opts.DowngradeThreshold = d;
        if (int.TryParse(cfg["ADAPTIVE_ERRORS_UPGRADE_MAX"], out var u) && u >= 0)
            opts.UpgradeMax = u;
    });

builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IChildRepository, ChildRepository>();
builder.Services.AddScoped<ICurriculumRepository, CurriculumRepository>();
builder.Services.AddScoped<ILearningRepository, LearningRepository>();
builder.Services.AddScoped<IBadgeRepository, BadgeRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IActivityLogRepository, ActivityLogRepository>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<CurrentUnitProgressService>();
builder.Services.AddScoped<ParentChildService>();
builder.Services.AddScoped<CatalogProgramResolver>();
builder.Services.AddScoped<CurriculumService>();
builder.Services.AddScoped<LearningService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<AdminService>();
builder.Services.AddScoped<IParentNotificationPublisher, ParentNotificationPublisher>();
builder.Services.AddScoped<IContentTranslationRepository, ContentTranslationRepository>();
builder.Services.AddScoped<IContentLocalizationService, ContentLocalizationService>();
builder.Services.AddScoped<CurriculumMapService>();
builder.Services.AddScoped<BadgeEvaluationJob>();
builder.Services.AddScoped<AdaptiveDifficultyJob>();
builder.Services.AddScoped<UnitCompletionFollowUpJob>();
builder.Services.AddScoped<StartupSeeder>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.RoutePrefix = "api/docs";
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "LearningPlatform API v1");
});

app.UseHttpsRedirection();
app.UseCors("FrontendCors");
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<ParentNotificationHub>("/hubs/parent-notifications");

using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<StartupSeeder>();
    var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
    await seeder.SeedAsync();
    recurringJobManager.AddOrUpdate<NotificationService>(
        "streak-reminders",
        s => s.CreateDailyStreakReminders(),
        "0 18 * * *");
    recurringJobManager.AddOrUpdate<NotificationService>(
        "weekly-summary",
        s => s.CreateWeeklySummaries(),
        "0 12 * * 0");
    recurringJobManager.AddOrUpdate<AdaptiveDifficultyJob>(
        "adaptive-difficulty-scheduled",
        j => j.ProcessScheduledCompletedUnitsAsync(),
        "*/15 * * * *");
    recurringJobManager.AddOrUpdate<BadgeEvaluationJob>(
        "badge-evaluation-all-children",
        j => j.EvaluateAllChildrenAsync(),
        "0 2 * * *");
}

app.Run();

public partial class Program;
