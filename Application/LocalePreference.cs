namespace LearningPlatform.Application;

/// <summary>Парсинг заголовка Accept-Language (G2/B6); основная локаль en или ru. Заголовок описан в OpenAPI (Swagger) на соответствующих GET.</summary>
public static class LocalePreference
{
    /// <summary>Возвращает <c>ru</c>, если первый языковой тег начинается с ru; иначе <c>en</c>.</summary>
    public static string Parse(string? acceptLanguage)
    {
        if (string.IsNullOrWhiteSpace(acceptLanguage))
            return "en";

        var first = acceptLanguage.Split(',')[0].Trim();
        var sep = first.IndexOf(';');
        if (sep >= 0)
            first = first[..sep].Trim();

        if (first.StartsWith("ru", StringComparison.OrdinalIgnoreCase))
            return "ru";

        return "en";
    }
}
