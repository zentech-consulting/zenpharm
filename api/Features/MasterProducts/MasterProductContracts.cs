using System.ComponentModel.DataAnnotations;

namespace Api.Features.MasterProducts;

public sealed record CreateMasterProductRequest
{
    [Required, MaxLength(50)]
    public string Sku { get; init; } = "";

    [Required, MaxLength(200)]
    public string Name { get; init; } = "";

    [Required, MaxLength(100)]
    public string Category { get; init; } = "";

    [MaxLength(1000)]
    public string? Description { get; init; }

    public decimal UnitPrice { get; init; }

    [MaxLength(20)]
    public string Unit { get; init; } = "each";

    [MaxLength(200)]
    public string? GenericName { get; init; }

    [MaxLength(200)]
    public string? Brand { get; init; }

    [MaxLength(50)]
    public string? Barcode { get; init; }

    [MaxLength(20)]
    public string ScheduleClass { get; init; } = "Unscheduled";

    [MaxLength(50)]
    public string? PackSize { get; init; }

    [MaxLength(1000)]
    public string? ActiveIngredients { get; init; }

    [MaxLength(2000)]
    public string? Warnings { get; init; }

    [MaxLength(20)]
    public string? PbsItemCode { get; init; }

    [MaxLength(500)]
    public string? ImageUrl { get; init; }
}

public sealed record UpdateMasterProductRequest
{
    [Required, MaxLength(200)]
    public string Name { get; init; } = "";

    [Required, MaxLength(100)]
    public string Category { get; init; } = "";

    [MaxLength(1000)]
    public string? Description { get; init; }

    public decimal UnitPrice { get; init; }

    [MaxLength(20)]
    public string Unit { get; init; } = "each";

    [MaxLength(200)]
    public string? GenericName { get; init; }

    [MaxLength(200)]
    public string? Brand { get; init; }

    [MaxLength(50)]
    public string? Barcode { get; init; }

    [MaxLength(20)]
    public string ScheduleClass { get; init; } = "Unscheduled";

    [MaxLength(50)]
    public string? PackSize { get; init; }

    [MaxLength(1000)]
    public string? ActiveIngredients { get; init; }

    [MaxLength(2000)]
    public string? Warnings { get; init; }

    [MaxLength(20)]
    public string? PbsItemCode { get; init; }

    [MaxLength(500)]
    public string? ImageUrl { get; init; }

    public bool IsActive { get; init; } = true;
}

public sealed record MasterProductDto(
    Guid Id,
    string Sku,
    string Name,
    string Category,
    string? Description,
    decimal UnitPrice,
    string Unit,
    string? GenericName,
    string? Brand,
    string? Barcode,
    string ScheduleClass,
    string? PackSize,
    string? ActiveIngredients,
    string? Warnings,
    string? PbsItemCode,
    string? ImageUrl,
    bool IsActive,
    DateTimeOffset CreatedAt);

public sealed record MasterProductListResponse(
    IReadOnlyList<MasterProductDto> Items,
    int TotalCount);
