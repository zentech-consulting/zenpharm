namespace Api.Features.Orders;

public interface IOrderManager
{
    Task<OrderDto> CreateGuestOrderAsync(CreateGuestOrderRequest request, CancellationToken ct = default);
    Task<OrderDto?> GetByOrderNumberAsync(string orderNumber, CancellationToken ct = default);
    Task<OrderDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<OrderListResponse> ListAsync(int page, int pageSize, string? status, string? search, CancellationToken ct = default);
    Task<OrderDto?> MarkAsReadyAsync(Guid id, CancellationToken ct = default);
    Task<OrderDto?> MarkAsCollectedAsync(Guid id, CancellationToken ct = default);
    Task<OrderDto?> CancelOrderAsync(Guid id, string reason, CancellationToken ct = default);
}
