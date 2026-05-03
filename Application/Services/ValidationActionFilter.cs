using FluentValidation;
using FluentValidation.Results;
using LearningPlatform.Application;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LearningPlatform.Application.Services;

public class ValidationActionFilter(IServiceProvider services) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var (_, argument) in context.ActionArguments)
        {
            if (argument is null) continue;

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            var validator = services.GetService(validatorType);
            if (validator is null) continue;

            var validateMethod = validatorType.GetMethod("ValidateAsync", [argument.GetType(), typeof(CancellationToken)]);
            if (validateMethod is null) continue;

            var task = (Task)validateMethod.Invoke(validator, [argument, context.HttpContext.RequestAborted])!;
            await task.ConfigureAwait(false);

            var resultProperty = task.GetType().GetProperty("Result");
            var result = resultProperty?.GetValue(task) as ValidationResult;
            if (result is null || result.IsValid) continue;

            var dict = result.Errors
                .GroupBy(e => string.IsNullOrEmpty(e.PropertyName) ? "_" : e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            throw new AppValidationException("Validation failed.", dict);
        }

        await next();
    }
}
