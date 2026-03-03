namespace Api.Features.Platform;

internal interface ISignupManager
{
    Task<CheckoutResponse> CreateCheckoutAsync(CreateCheckoutRequest request, CancellationToken ct = default);
    Task<SignupStatusResponse> GetStatusAsync(string stripeSessionId, CancellationToken ct = default);
    Task<PendingSignupEntity?> FindByStripeSessionAsync(string sessionId, CancellationToken ct = default);
    Task UpdateStatusAsync(Guid signupId, string status, Guid? tenantId = null, string? failureReason = null, string? stripeCustomerId = null, string? stripeSubscriptionId = null, CancellationToken ct = default);
    Task<bool> CheckSubdomainAsync(string subdomain, CancellationToken ct = default);
    Task<IReadOnlyList<PlanSummary>> ListPlansAsync(CancellationToken ct = default);
}
