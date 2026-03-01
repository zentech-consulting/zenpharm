namespace Api.Features.MasterProducts;

public interface IMasterProductManager
{
    Task<MasterProductDto> CreateAsync(CreateMasterProductRequest request, CancellationToken ct = default);
    Task<MasterProductDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<MasterProductListResponse> ListAsync(int page, int pageSize, string? category, string? search, string? scheduleClass, CancellationToken ct = default);
    Task<MasterProductDto?> UpdateAsync(Guid id, UpdateMasterProductRequest request, CancellationToken ct = default);
}
