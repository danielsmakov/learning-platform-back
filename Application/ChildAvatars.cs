namespace LearningPlatform.Application;

/// <summary>Фиксированный набор аватаров ребёнка (id хранится в <see cref="Domain.Child.AvatarUrl"/>).</summary>
public static class ChildAvatars
{
    public const string DefaultId = "avatar-01";

    private static readonly HashSet<string> Allowed = new(StringComparer.Ordinal)
    {
        "avatar-01",
        "avatar-02",
        "avatar-03",
        "avatar-04",
        "avatar-05",
        "avatar-06",
        "avatar-07",
        "avatar-08",
        "avatar-09",
        "avatar-10",
    };

    public static bool IsAllowed(string? avatarId) =>
        !string.IsNullOrWhiteSpace(avatarId) && Allowed.Contains(avatarId.Trim());

    public static string Normalize(string? avatarId) =>
        IsAllowed(avatarId) ? avatarId!.Trim() : DefaultId;
}
