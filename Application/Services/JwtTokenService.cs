using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LearningPlatform.Domain;
using Microsoft.IdentityModel.Tokens;

namespace LearningPlatform.Application.Services;

public class JwtTokenService(IConfiguration configuration)
{
    private readonly IConfiguration _configuration = configuration;

    public static TokenValidationParameters GetValidationParameters(IConfiguration configuration)
    {
        var issuer = configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer is not configured.");
        var audience = configuration["Jwt:Audience"] ?? throw new InvalidOperationException("Jwt:Audience is not configured.");
        var key = configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured. Set Jwt__Key in environment or .env.");
        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    }

    public (string token, DateTime expiresAt) GenerateAccessToken(User user)
    {
        var minutes = _configuration.GetValue("Jwt:AccessTokenExpirationMinutes", 15);
        if (minutes < 1) minutes = 15;
        var expiresAt = DateTime.UtcNow.AddMinutes(minutes);
        var key = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured. Set Jwt__Key in environment or .env.");
        var issuer = _configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer is not configured.");
        var audience = _configuration["Jwt:Audience"] ?? throw new InvalidOperationException("Jwt:Audience is not configured.");

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString())
        };

        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(issuer, audience, claims, expires: expiresAt, signingCredentials: credentials);
        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    public string GenerateRefreshToken() => Convert.ToBase64String(Guid.NewGuid().ToByteArray()) + Convert.ToBase64String(Guid.NewGuid().ToByteArray());
}
