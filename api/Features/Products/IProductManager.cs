namespace Api.Features.Products;

public interface IProductManager
{
    Task<ProductDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ProductListResponse> ListAsync(int page, int pageSize, string? search, bool lowStockOnly, bool expiringOnly, CancellationToken ct = default);
    Task<IReadOnlyList<ProductDto>> ImportFromCatalogueAsync(Guid[] masterProductIds, CancellationToken ct = default);
    Task<ProductDto?> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<StockMovementDto> RecordStockMovementAsync(Guid productId, RecordStockMovementRequest request, CancellationToken ct = default);
    Task<StockMovementListResponse> ListStockMovementsAsync(Guid productId, int page, int pageSize, CancellationToken ct = default);
    Task<LowStockSummary> GetLowStockSummaryAsync(CancellationToken ct = default);
    Task<ExpiryAlertResponse> GetExpiryAlertsAsync(int daysAhead, CancellationToken ct = default);
}
