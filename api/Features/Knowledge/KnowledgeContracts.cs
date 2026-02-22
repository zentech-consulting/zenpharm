using System.ComponentModel.DataAnnotations;

namespace Api.Features.Knowledge;

public sealed record CreateKnowledgeEntryRequest
{
    [Required, MaxLength(200)]
    public string Title { get; init; } = "";

    [Required]
    public string Content { get; init; } = "";

    [MaxLength(50)]
    public string Category { get; init; } = "general";

    public IReadOnlyList<string>? Tags { get; init; }
}

public sealed record UpdateKnowledgeEntryRequest
{
    [Required, MaxLength(200)]
    public string Title { get; init; } = "";

    [Required]
    public string Content { get; init; } = "";

    [MaxLength(50)]
    public string Category { get; init; } = "general";

    public IReadOnlyList<string>? Tags { get; init; }
}

public sealed record KnowledgeEntryDto(
    Guid Id,
    string Title,
    string Content,
    string Category,
    IReadOnlyList<string> Tags,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record KnowledgeListResponse(
    IReadOnlyList<KnowledgeEntryDto> Items,
    int TotalCount);

public sealed record KnowledgeSearchRequest
{
    [Required, MaxLength(500)]
    public string Query { get; init; } = "";

    public int MaxResults { get; init; } = 5;
}

public sealed record KnowledgeSearchResult(
    KnowledgeEntryDto Entry,
    double Score);
