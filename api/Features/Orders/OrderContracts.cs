using System.ComponentModel.DataAnnotations;

namespace Api.Features.Orders;

public sealed record CreateGuestOrderRequest
{
    [Required, MaxLength(100)]
    public string FirstName { get; init; } = "";

    [Required, MaxLength(100)]
    public string LastName { get; init; } = "";

    [Required, EmailAddress, MaxLength(200)]
    public string Email { get; init; } = "";

    [Required, MaxLength(20)]
    public string Phone { get; init; } = "";

    [MaxLength(2000)]
    public string? Notes { get; init; }

    [Required, MinLength(1)]
    public OrderItemRequest[] Items { get; init; } = [];
}

public sealed record OrderItemRequest
{
    public Guid ProductId { get; init; }

    [Range(1, 999)]
    public int Quantity { get; init; }
}

public sealed record OrderDto(
    Guid Id,
    string OrderNumber,
    Guid ClientId,
    string ClientName,
    string Status,
    decimal Subtotal,
    decimal TaxAmount,
    decimal Total,
    string? Notes,
    DateTimeOffset? EstimatedReadyAt,
    DateTimeOffset? ReadyNotifiedAt,
    DateTimeOffset? CollectedAt,
    DateTimeOffset? CancelledAt,
    string? CancellationReason,
    DateTimeOffset CreatedAt,
    IReadOnlyList<OrderItemDto> Items);

public sealed record OrderItemDto(
    Guid Id,
    Guid TenantProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal Subtotal);

public sealed record OrderListResponse(
    IReadOnlyList<OrderSummaryDto> Items,
    int TotalCount);

public sealed record OrderSummaryDto(
    Guid Id,
    string OrderNumber,
    string ClientName,
    string Status,
    int ItemCount,
    decimal Total,
    DateTimeOffset? EstimatedReadyAt,
    DateTimeOffset CreatedAt);

public sealed record CancelOrderRequest
{
    [Required, MaxLength(500)]
    public string Reason { get; init; } = "";
}
