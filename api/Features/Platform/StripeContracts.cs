namespace Api.Features.Platform;

// Legacy StripeWebhookEvent types kept for backward compatibility with existing tests.
// The actual webhook handler now uses Stripe.net SDK types (Stripe.Event).

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
