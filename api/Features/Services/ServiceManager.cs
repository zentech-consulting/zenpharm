using Api.Common;

namespace Api.Features.Services;

internal sealed class ServiceManager(
    ITenantDb db,
    ILogger<ServiceManager> logger) : IServiceManager
{
    public Task<ServiceDto> CreateAsync(CreateServiceRequest request, CancellationToken ct = default)
    {
        throw new NotImplementedException("Service module not yet implemented — see Phase 1, Subtask 4");
    }

    public Task<ServiceDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        throw new NotImplementedException("Service module not yet implemented — see Phase 1, Subtask 4");
    }

    public Task<ServiceListResponse> ListAsync(int page, int pageSize, string? category, CancellationToken ct = default)
    {
        throw new NotImplementedException("Service module not yet implemented — see Phase 1, Subtask 4");
    }

    public Task<ServiceDto?> UpdateAsync(Guid id, UpdateServiceRequest request, CancellationToken ct = default)
    {
        throw new NotImplementedException("Service module not yet implemented — see Phase 1, Subtask 4");
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        throw new NotImplementedException("Service module not yet implemented — see Phase 1, Subtask 4");
    }
}
