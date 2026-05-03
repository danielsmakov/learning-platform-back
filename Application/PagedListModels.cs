using System.Text.Json.Serialization;

namespace LearningPlatform.Application;

/// <summary>H1: единый ответ списка с метаданными пагинации (JSON: snake_case для полей страницы).</summary>
public class PagedResponse<T>
{
    public IReadOnlyCollection<T> Items { get; init; } = [];

    [JsonPropertyName("total")]
    public int Total { get; init; }

    [JsonPropertyName("page")]
    public int Page { get; init; }

    [JsonPropertyName("page_size")]
    public int PageSize { get; init; }

    [JsonPropertyName("total_pages")]
    public int TotalPages { get; init; }
}
