namespace Api.Features.Platform;

internal sealed class ProvisioningPipeline(
    ILogger<ProvisioningPipeline> logger) : IProvisioningPipeline
{
    public Task<ProvisionResult> ProvisionAsync(ProvisionRequest request, CancellationToken ct = default)
    {
        logger.LogInformation(
            "Provisioning tenant: {Name} ({Subdomain}), admin: {Email}, plan: {Plan}",
            request.TenantName, request.Subdomain, request.AdminEmail, request.Plan ?? "free");

        // Stub — in production this would:
        // 1. Create Stripe customer + subscription
        // 2. Create tenant database
        // 3. Run migrations
        // 4. Seed admin user
        // 5. Update catalogue DB

        var tenantId = Guid.NewGuid().ToString();

        logger.LogInformation("Tenant provisioned (stub): {TenantId}", tenantId);

        return Task.FromResult(new ProvisionResult(
            Success: true,
            TenantId: tenantId,
            Message: "Tenant provisioned successfully (stub)"));
    }
}
