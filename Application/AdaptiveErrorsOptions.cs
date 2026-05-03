namespace LearningPlatform.Application;

/// <summary>Пороги адаптивной смены дорожки по числу ошибок за завершённый юнит (E3).</summary>
public class AdaptiveErrorsOptions
{
    public const string SectionName = "AdaptiveErrors";

    /// <summary>При ErrorCount &gt;= порога — переход на более лёгкую программу (E4).</summary>
    public int DowngradeThreshold { get; set; } = 8;

    /// <summary>При ErrorCount &lt;= этого значения — допускается переход на более сложную программу («меньше N ошибок за юнит»).</summary>
    public int UpgradeMax { get; set; } = 2;
}
