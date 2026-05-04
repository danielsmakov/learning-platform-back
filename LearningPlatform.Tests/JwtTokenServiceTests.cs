using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using LearningPlatform.Application.Services;
using LearningPlatform.Domain;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace LearningPlatform.Tests;

public class JwtTokenServiceTests
{
    private static IConfiguration CreateConfig(string key = "0123456789abcdef0123456789abcdef")
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = key,
                ["Jwt:Issuer"] = "LearningPlatform",
                ["Jwt:Audience"] = "LearningPlatformClient",
                ["Jwt:AccessTokenExpirationMinutes"] = "15"
            })
            .Build();
    }

    [Fact]
    public void GetValidationParameters_throws_when_key_missing()
    {
        var empty = new ConfigurationBuilder().Build();
        Assert.Throws<InvalidOperationException>(() => JwtTokenService.GetValidationParameters(empty));
    }

    [Fact]
    public void GetValidationParameters_returns_symmetric_key_when_configured()
    {
        var p = JwtTokenService.GetValidationParameters(CreateConfig());
        Assert.NotNull(p.IssuerSigningKey);
        Assert.Equal("LearningPlatform", p.ValidIssuer);
    }

    [Fact]
    public void GenerateAccessToken_round_trips_validation_and_contains_role_claim()
    {
        var config = CreateConfig();
        var svc = new JwtTokenService(config);
        var user = new User
        {
            Id = Guid.Parse("a1000000-0000-4000-8000-000000000001"),
            Email = "parent@test.dev",
            Role = UserRole.Parent
        };

        var (token, _) = svc.GenerateAccessToken(user);

        var handler = new JwtSecurityTokenHandler();
        var principal = handler.ValidateToken(
            token,
            JwtTokenService.GetValidationParameters(config),
            out _);

        Assert.Equal(user.Id.ToString(), principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        Assert.Equal(UserRole.Parent.ToString(), principal.FindFirst(ClaimTypes.Role)?.Value);
    }

    [Fact]
    public void GenerateRefreshToken_returns_non_empty_base64_like_string()
    {
        var svc = new JwtTokenService(CreateConfig());
        var r = svc.GenerateRefreshToken();
        Assert.False(string.IsNullOrWhiteSpace(r));
        Assert.True(r.Length > 20);
    }
}
