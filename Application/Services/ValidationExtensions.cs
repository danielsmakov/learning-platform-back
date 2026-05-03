using FluentValidation;

namespace LearningPlatform.Application.Services;

public static class ValidationExtensions
{
    public static async Task ValidateAndThrowAsync<T>(this IValidator<T> validator, T model, CancellationToken ct = default)
    {
        var result = await validator.ValidateAsync(model, ct);
        if (!result.IsValid)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.ErrorMessage));
            throw new InvalidOperationException(errors);
        }
    }
}
