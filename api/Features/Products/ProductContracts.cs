using System.ComponentModel.DataAnnotations;

namespace Api.Features.Products;

public sealed record ProductDto(
    Guid Id,
    Guid MasterProductId,
    string MasterProductName,
    string? GenericName,
    string? Brand,
    string Category,
    string ScheduleClass,
    decimal DefaultPrice,
    string? CustomName,
    decimal? CustomPrice,
    string? ImageUrl,
    int StockQuantity,
    int ReorderLevel,
    DateOnly? ExpiryDate,
    bool IsVisible,
    bool IsFeatured,
    int SortOrder,
    DateTimeOffset CreatedAt);

public sealed record ProductListResponse(
    IReadOnlyList<ProductDto> Items,
    int TotalCount);

public sealed record ImportProductsRequest
{
    [Required, MinLength(1), MaxLength(100)]
    public Guid[] MasterProductIds { get; init; } = [];
}

public sealed record UpdateProductRequest
{
    [MaxLength(200)]
    public string? CustomName { get; init; }

    public decimal? CustomPrice { get; init; }

    public int ReorderLevel { get; init; } = 10;

    public DateOnly? ExpiryDate { get; init; }

    public bool IsVisible { get; init; } = true;

    public bool IsFeatured { get; init; }

    public int SortOrder { get; init; }
}

public sealed record RecordStockMovementRequest
{
    [Required, MaxLength(20)]
    public string MovementType { get; init; } = "stock_in";

    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
    public int Quantity { get; init; }

    [MaxLength(200)]
    public string? Reference { get; init; }

    [MaxLength(2000)]
    public string? Notes { get; init; }

    [MaxLength(200)]
    public string? CreatedBy { get; init; }

    [MaxLength(200)]
    public string? ApprovedBy { get; init; }
}

public sealed record StockMovementDto(
    Guid Id,
    Guid TenantProductId,
    string MovementType,
    int Quantity,
    string? Reference,
    string? Notes,
    DateTimeOffset CreatedAt,
    string? CreatedBy,
    string? ApprovedBy);

public sealed record StockMovementListResponse(
    IReadOnlyList<StockMovementDto> Items,
    int TotalCount);

public sealed record LowStockSummary(
    int TotalProducts,
    int LowStockCount,
    IReadOnlyList<ProductDto> LowStockItems);

public sealed record ExpiryAlertResponse(
    int ExpiringCount,
    IReadOnlyList<ProductDto> ExpiringItems);
