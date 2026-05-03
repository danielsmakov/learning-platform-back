using LearningPlatform.Application;

namespace LearningPlatform.Infrastructure;

public class ErrorHandlingMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            context.Response.ContentType = "application/json";
            int code;
            object? details = null;
            string message;

            switch (ex)
            {
                case AppValidationException vex:
                    code = StatusCodes.Status422UnprocessableEntity;
                    message = vex.Message;
                    details = new { errors = vex.Errors };
                    break;
                case AppUnauthorizedException uex:
                    code = StatusCodes.Status401Unauthorized;
                    message = uex.Message;
                    break;
                case AppForbiddenException fex:
                    code = StatusCodes.Status403Forbidden;
                    message = fex.Message;
                    break;
                case InvalidOperationException:
                    code = StatusCodes.Status400BadRequest;
                    message = ex.Message;
                    break;
                case KeyNotFoundException:
                    code = StatusCodes.Status404NotFound;
                    message = ex.Message;
                    break;
                default:
                    code = StatusCodes.Status500InternalServerError;
                    message = ex.Message;
                    details = "Unexpected server error.";
                    break;
            }

            context.Response.StatusCode = code;
            await context.Response.WriteAsync(ApiErrorJson.Serialize(message, details));
        }
    }
}
