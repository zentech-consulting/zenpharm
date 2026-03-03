namespace Api.Features.Shop;

public sealed record ShopProductDto(
    Guid Id,
    string Name,
    string? GenericName,
    string? Brand,
    string Category,
    string ScheduleClass,
    decimal Price,
    string? ImageUrl,
    string StockAvailability,
    bool IsFeatured);

public sealed record ShopProductDetailDto(
    Guid Id,
    string Name,
    string? GenericName,
    string? Brand,
    string Category,
    string ScheduleClass,
    decimal Price,
    string? ImageUrl,
    string StockAvailability,
    bool IsFeatured,
    string? ActiveIngredients,
    string? Warnings,
    string? Description);

public sealed record ShopProductListResponse(
    IReadOnlyList<ShopProductDto> Items,
    int TotalCount);
