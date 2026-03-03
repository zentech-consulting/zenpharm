namespace Api.Features.Shop;

public interface IShopManager
{
    Task<ShopProductListResponse> ListProductsAsync(
        string? category, string? search, bool? featured,
        int page, int pageSize, CancellationToken ct = default);

    Task<ShopProductDetailDto?> GetProductAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<string>> ListCategoriesAsync(CancellationToken ct = default);
}
