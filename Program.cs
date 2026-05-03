using Hangfire;
using Hangfire.PostgreSql;
using FluentValidation;
using LearningPlatform;
using LearningPlatform.Application;
using LearningPlatform.Application.Services;
using LearningPlatform.Application.Hangfire;
using LearningPlatform.Infrastructure;
using LearningPlatform.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
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
            "**REST under `/api/v1/…`** — programs, units, lessons, exercises (catalog); `GET /api/v1/children/{id}/curriculum-map` (B1 curriculum map). " +
            "Query `programId` or `childId` for catalog context; admin may use `all=true` on catalog lists; admin-only text filter: `search` on `GET /api/v1/lessons`. " +
            "List responses use pagination fields `items`, `total`, `page`, `page_size`, `total_pages`. " +
            "**Accept-Language (G2):** optional on catalog GETs and curriculum-map; send `ru`, `en`, or tags like `ru-RU;q=0.9,en;q=0.8` — first tag wins (`ru*` → Russian, else English). " +
            "**Errors (A6):** JSON `{ \"message\", \"details\" }` — 401 not authenticated, 403 forbidden, 422 validation (`details.errors` field map), 400/404/500 as applicable. " +
            "**SignalR (G4, not REST):** hub path `/hubs/parent-notifications`; role **Parent**; JWT via query `access_token` or header `Authorization: Bearer` (same as REST). " +
            "Server event name `notification` (see `ParentNotificationPublisher.HubEventName`); payload shape: `id` (uuid), `type` (NotificationType enum int), `title`, `body`, `childId` (uuid|null), `createdAt` (ISO 8601), `isRead` (bool) — same as `ParentNotificationPublisher.ParentNotificationPayload`."
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
        // H3 / G4: SignalR — JWT в query `access_token` или заголовок (см. OpenAPI description выше).
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
builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, JsonAuthorizationMiddlewareResultHandler>();
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
    RecurringJobsRegistration.RegisterP3C3Jobs(recurringJobManager);
}

app.Run();

public partial class Program;
