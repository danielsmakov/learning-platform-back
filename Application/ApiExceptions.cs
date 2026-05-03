namespace LearningPlatform.Application;

/// <summary>A6: 401 — не аутентифицирован (неверные учётные данные, недействительный refresh и т.п.).</summary>
public sealed class AppUnauthorizedException(string message) : Exception(message);

/// <summary>A6: 403 — аутентифицирован, но нет прав на ресурс или роль.</summary>
public sealed class AppForbiddenException(string message) : Exception(message);

/// <summary>A6: 422 — ошибки валидации; <see cref="Errors"/> уходит в JSON как часть <c>details</c>.</summary>
public sealed class AppValidationException(string message, IReadOnlyDictionary<string, string[]> errors) : Exception(message)
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}
