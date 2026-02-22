namespace Api.Features.Clients;

public interface IClientManager
{
    Task<ClientDto> CreateAsync(CreateClientRequest request, CancellationToken ct = default);
    Task<ClientDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ClientListResponse> ListAsync(int page, int pageSize, string? search, CancellationToken ct = default);
    Task<ClientDto?> UpdateAsync(Guid id, UpdateClientRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
