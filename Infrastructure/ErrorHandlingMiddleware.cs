using System.Text.Json;

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
            context.Response.StatusCode = ex switch
            {
                UnauthorizedAccessException => StatusCodes.Status403Forbidden,
                InvalidOperationException => StatusCodes.Status400BadRequest,
                KeyNotFoundException => StatusCodes.Status404NotFound,
                _ => StatusCodes.Status500InternalServerError
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                message = ex.Message,
                details = context.Response.StatusCode == 500 ? "Unexpected server error." : null
            }));
        }
    }
}
