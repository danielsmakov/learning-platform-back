using LearningPlatform.Application;
using Xunit;

namespace LearningPlatform.Tests;

public class LocalePreferenceTests
{
    [Fact]
    public void Parse_null_header_returns_en()
    {
        Assert.Equal("en", LocalePreference.Parse(null));
    }

    [Theory]
    [InlineData("", "en")]
    [InlineData("   ", "en")]
    [InlineData("en-US", "en")]
    [InlineData("en, ru;q=0.9", "en")]
    [InlineData("ru", "ru")]
    [InlineData("ru-RU", "ru")]
    [InlineData("RU", "ru")]
    [InlineData("ru-RU;q=0.9,en;q=0.8", "ru")]
    public void Parse_returns_expected_locale(string? header, string expected)
    {
        Assert.Equal(expected, LocalePreference.Parse(header));
    }
}
