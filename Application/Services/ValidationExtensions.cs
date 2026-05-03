using FluentValidation;
using LearningPlatform.Application;

namespace LearningPlatform.Application.Services;

public static class ValidationExtensions
{
    public static async Task ValidateAndThrowAsync<T>(this IValidator<T> validator, T model, CancellationToken ct = default)
    {
        var result = await validator.ValidateAsync(model, ct);
        if (!result.IsValid)
        {
            var dict = result.Errors
                .GroupBy(e => string.IsNullOrEmpty(e.PropertyName) ? "_" : e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            throw new AppValidationException("Validation failed.", dict);
        }
    }
}
