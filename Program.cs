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

EnvBootstrap.LoadLocalEnvFile();
var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
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
    });
builder.Services.AddAuthorization();

builder.Services.AddHangfire(h =>
{
    h.UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UsePostgreSqlStorage(c => c.UseNpgsqlConnection(config.GetConnectionString("DefaultConnection")));
});
builder.Services.AddHangfireServer();

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
builder.Services.AddScoped<ParentChildService>();
builder.Services.AddScoped<CurriculumService>();
builder.Services.AddScoped<LearningService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<AdminService>();
builder.Services.AddScoped<BadgeEvaluationJob>();
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

using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<StartupSeeder>();
    var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
    await seeder.SeedAsync();
    recurringJobManager.AddOrUpdate<NotificationService>(
        "streak-reminders",
        s => s.CreateDailyStreakReminders(),
        "0 23 * * *");
    recurringJobManager.AddOrUpdate<NotificationService>(
        "weekly-summary",
        s => s.CreateWeeklySummaries(),
        "0 12 * * 0");
}

app.Run();

public partial class Program;
