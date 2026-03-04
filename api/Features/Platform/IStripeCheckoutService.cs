namespace Api.Features.Platform;

/// <summary>
/// Thin wrapper around Stripe's Checkout Session API for testability.
/// </summary>
internal interface IStripeCheckoutService
{
    Task<CheckoutResponse> CreateSessionAsync(
        CreateCheckoutRequest request,
        Guid signupId,
        string planName,
        decimal price,
        CancellationToken ct = default);
}
