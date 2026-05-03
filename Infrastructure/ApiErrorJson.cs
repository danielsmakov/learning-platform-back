using System.Text.Json;
using System.Text.Json.Serialization;

namespace LearningPlatform.Infrastructure;

/// <summary>A6: единый контракт тела ошибки для middleware и JWT challenge/forbidden.</summary>
public static class ApiErrorJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static string Serialize(string message, object? details = null) =>
        JsonSerializer.Serialize(new { message, details }, Options);
}
