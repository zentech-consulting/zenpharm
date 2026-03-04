namespace Api.Features.Platform;

public interface IProvisioningPipeline
{
    Task<ProvisionResult> ProvisionAsync(ProvisionRequest request, CancellationToken ct = default);
}

public sealed record ProvisionRequest(
    string TenantName,
    string Subdomain,
    string AdminEmail,
    string? Plan,
    string? AdminFullName = null,
    Guid? PlanId = null,
    string? BillingPeriod = "Monthly",
    string? StripeCustomerId = null,
    string? StripeSubscriptionId = null,
    string? TemplatePack = null);

public sealed record ProvisionResult(
    bool Success,
    string? TenantId,
    string? Message,
    string? AdminPassword = null);
