using System.Security.Cryptography;
using System.Text;

namespace LearningPlatform.Application.Services;

public static class RefreshTokenHash
{
    public static string ComputeLookup(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(bytes);
    }
}
