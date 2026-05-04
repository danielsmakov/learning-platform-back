using System.Text.Json;
using LearningPlatform.Infrastructure;
using Xunit;

namespace LearningPlatform.Tests;

public class ApiErrorJsonTests
{
    [Fact]
    public void Serialize_message_only_produces_camelCase_json()
    {
        var json = ApiErrorJson.Serialize("Not found");
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.Equal("Not found", root.GetProperty("message").GetString());
        Assert.False(root.TryGetProperty("details", out _));
    }

    [Fact]
    public void Serialize_includes_details_when_present()
    {
        var json = ApiErrorJson.Serialize("Validation failed", new { field = "email" });
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("Validation failed", doc.RootElement.GetProperty("message").GetString());
        Assert.Equal("email", doc.RootElement.GetProperty("details").GetProperty("field").GetString());
    }
}
