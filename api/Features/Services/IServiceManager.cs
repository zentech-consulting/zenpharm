namespace Api.Features.Services;

public interface IServiceManager
{
    Task<ServiceDto> CreateAsync(CreateServiceRequest request, CancellationToken ct = default);
    Task<ServiceDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ServiceListResponse> ListAsync(int page, int pageSize, string? category, CancellationToken ct = default);
    Task<ServiceDto?> UpdateAsync(Guid id, UpdateServiceRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
