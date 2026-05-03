using FluentValidation;
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
            var result = resultProperty?.GetValue(task);
            var isValid = (bool?)result?.GetType().GetProperty("IsValid")?.GetValue(result) ?? true;
            if (isValid) continue;

            var errors = result?.GetType().GetProperty("Errors")?.GetValue(result) as IEnumerable<object>;
            var messages = errors?.Select(e => e.GetType().GetProperty("ErrorMessage")?.GetValue(e)?.ToString()).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray()
                           ?? ["Validation failed."];
            throw new InvalidOperationException(string.Join("; ", messages));
        }

        await next();
    }
}
