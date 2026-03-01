namespace Api.Features.Platform;

public sealed record StripeWebhookEvent
{
    public string Id { get; init; } = "";
    public string Type { get; init; } = "";
    public StripeEventData Data { get; init; } = new();
}

public sealed record StripeEventData
{
    public StripeEventObject Object { get; init; } = new();
}

public sealed record StripeEventObject
{
    public string Id { get; init; } = "";
    public string? Customer { get; init; }
    public string? Status { get; init; }
}
