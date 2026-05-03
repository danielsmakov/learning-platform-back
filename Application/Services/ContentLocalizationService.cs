using LearningPlatform.Domain;
using LearningPlatform.Infrastructure.Repositories;

namespace LearningPlatform.Application.Services;

/// <summary>B6: подстановка переводов для каталога (fallback en → ru → значение в БД).</summary>
public interface IContentLocalizationService
{
    Task ApplyUnitsAsync(IReadOnlyList<Unit> units, string locale);
    Task ApplyLessonsAsync(IReadOnlyList<Lesson> lessons, string locale);
    Task ApplyProgramsAsync(IReadOnlyList<LearningProgram> programs, string locale);
    Task ApplyExercisesAsync(IReadOnlyList<Exercise> exercises, string locale);
    Task<(string Title, string Description)> GetLocalizedProgramAsync(LearningProgram program, string locale);
}

public class ContentLocalizationService(IContentTranslationRepository translations) : IContentLocalizationService
{
    public async Task ApplyUnitsAsync(IReadOnlyList<Unit> units, string locale)
    {
        if (units.Count == 0) return;
        var ids = units.Select(u => u.Id).ToList();
        var rows = await translations.ListAsync(ContentEntityTypes.Unit, ids);
        foreach (var u in units)
        {
            u.Title = Resolve(u.Title, u.Id, ContentFields.Title, locale, rows);
            u.Description = Resolve(u.Description, u.Id, ContentFields.Description, locale, rows);
        }
    }

    public async Task ApplyLessonsAsync(IReadOnlyList<Lesson> lessons, string locale)
    {
        if (lessons.Count == 0) return;
        var ids = lessons.Select(l => l.Id).ToList();
        var rows = await translations.ListAsync(ContentEntityTypes.Lesson, ids);
        foreach (var l in lessons)
            l.Title = Resolve(l.Title, l.Id, ContentFields.Title, locale, rows);
    }

    public async Task ApplyProgramsAsync(IReadOnlyList<LearningProgram> programs, string locale)
    {
        if (programs.Count == 0) return;
        var ids = programs.Select(p => p.Id).ToList();
        var rows = await translations.ListAsync(ContentEntityTypes.Program, ids);
        foreach (var p in programs)
        {
            p.Title = Resolve(p.Title, p.Id, ContentFields.Title, locale, rows);
            p.Description = Resolve(p.Description, p.Id, ContentFields.Description, locale, rows);
        }
    }

    public async Task ApplyExercisesAsync(IReadOnlyList<Exercise> exercises, string locale)
    {
        if (exercises.Count == 0) return;
        var ids = exercises.Select(e => e.Id).ToList();
        var rows = await translations.ListAsync(ContentEntityTypes.Exercise, ids);
        foreach (var e in exercises)
            e.Content = Resolve(e.Content, e.Id, ContentFields.Content, locale, rows);
    }

    public async Task<(string Title, string Description)> GetLocalizedProgramAsync(LearningProgram program, string locale)
    {
        var rows = await translations.ListAsync(ContentEntityTypes.Program, [program.Id]);
        return (
            Resolve(program.Title, program.Id, ContentFields.Title, locale, rows),
            Resolve(program.Description, program.Id, ContentFields.Description, locale, rows));
    }

    private static string Resolve(string defaultValue, Guid entityId, string fieldName, string locale, List<ContentTranslation> rows)
    {
        var match = rows.Where(x => x.EntityId == entityId && x.FieldName == fieldName).ToList();
        if (match.Count == 0)
            return defaultValue;

        string? ru = match.FirstOrDefault(x => x.Locale.Equals("ru", StringComparison.OrdinalIgnoreCase))?.Value;
        string? en = match.FirstOrDefault(x => x.Locale.Equals("en", StringComparison.OrdinalIgnoreCase))?.Value;

        if (locale == "ru")
            return FirstNonEmpty(ru, en, defaultValue);

        return FirstNonEmpty(en, ru, defaultValue);
    }

    private static string FirstNonEmpty(params string?[] candidates)
    {
        foreach (var c in candidates)
        {
            if (!string.IsNullOrWhiteSpace(c))
                return c!;
        }

        return string.Empty;
    }
}

public static class ContentEntityTypes
{
    public const string Program = "Program";
    public const string Unit = "Unit";
    public const string Lesson = "Lesson";
    public const string Exercise = "Exercise";
}

public static class ContentFields
{
    public const string Title = "Title";
    public const string Description = "Description";
    public const string Content = "Content";
}
