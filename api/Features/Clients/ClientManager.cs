using Api.Common;

namespace Api.Features.Clients;

internal sealed class ClientManager(
    ITenantDb db,
    ILogger<ClientManager> logger) : IClientManager
{
    public Task<ClientDto> CreateAsync(CreateClientRequest request, CancellationToken ct = default)
    {
        throw new NotImplementedException("Client module not yet implemented — see Phase 1, Subtask 3");
    }

    public Task<ClientDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        throw new NotImplementedException("Client module not yet implemented — see Phase 1, Subtask 3");
    }

    public Task<ClientListResponse> ListAsync(int page, int pageSize, string? search, CancellationToken ct = default)
    {
        throw new NotImplementedException("Client module not yet implemented — see Phase 1, Subtask 3");
    }

    public Task<ClientDto?> UpdateAsync(Guid id, UpdateClientRequest request, CancellationToken ct = default)
    {
        throw new NotImplementedException("Client module not yet implemented — see Phase 1, Subtask 3");
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        throw new NotImplementedException("Client module not yet implemented — see Phase 1, Subtask 3");
    }
}
