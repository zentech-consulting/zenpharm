using System.ComponentModel.DataAnnotations;

namespace Api.Features.Services;

public sealed record CreateServiceRequest
{
    [Required, MaxLength(200)]
    public string Name { get; init; } = "";

    public string? Description { get; init; }

    [Required, MaxLength(50)]
    public string Category { get; init; } = "";

    [Range(0, double.MaxValue)]
    public decimal Price { get; init; }

    public int DurationMinutes { get; init; } = 30;

    public bool IsActive { get; init; } = true;
}

public sealed record UpdateServiceRequest
{
    [Required, MaxLength(200)]
    public string Name { get; init; } = "";

    public string? Description { get; init; }

    [Required, MaxLength(50)]
    public string Category { get; init; } = "";

    [Range(0, double.MaxValue)]
    public decimal Price { get; init; }

    public int DurationMinutes { get; init; } = 30;

    public bool IsActive { get; init; } = true;
}

public sealed record ServiceDto(
    Guid Id,
    string Name,
    string? Description,
    string Category,
    decimal Price,
    int DurationMinutes,
    bool IsActive,
    DateTimeOffset CreatedAt);

public sealed record ServiceListResponse(
    IReadOnlyList<ServiceDto> Items,
    int TotalCount);
