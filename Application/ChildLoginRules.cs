using System.Text.RegularExpressions;

namespace LearningPlatform.Application;

/// <summary>Правила логина и PIN ребёнка (вход и профиль).</summary>
public static partial class ChildLoginRules
{
    public const int LoginMinLength = 3;
    public const int LoginMaxLength = 32;

    [GeneratedRegex("^[a-z0-9_]+$", RegexOptions.CultureInvariant)]
    private static partial Regex LoginPattern();

    public static string NormalizeLogin(string login) =>
        login.Trim().ToLowerInvariant();

    public static bool IsValidLoginFormat(string login)
    {
        var n = NormalizeLogin(login);
        return n.Length >= LoginMinLength && n.Length <= LoginMaxLength && LoginPattern().IsMatch(n);
    }

    public static bool IsValidPinFormat(string pin) =>
        pin.Length == 4 && pin.All(char.IsDigit);
}
