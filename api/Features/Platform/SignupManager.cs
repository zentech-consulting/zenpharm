using Api.Common;
using Dapper;

namespace Api.Features.Platform;

internal sealed class SignupManager(
    ICatalogDb catalogDb,
    IStripeCheckoutService stripeService,
    ILogger<SignupManager> logger) : ISignupManager
{
    public async Task<CheckoutResponse> CreateCheckoutAsync(CreateCheckoutRequest request, CancellationToken ct = default)
    {
        if (!SignupValidation.IsValidSubdomain(request.Subdomain))
            throw new ArgumentException("Invalid subdomain format.");

        if (SignupValidation.IsReservedSubdomain(request.Subdomain))
            throw new ArgumentException($"The subdomain '{request.Subdomain}' is reserved.");

        var available = await CheckSubdomainAsync(request.Subdomain, ct);
        if (!available)
            throw new ArgumentException($"The subdomain '{request.Subdomain}' is already taken.");

        using var conn = await catalogDb.CreateAsync();

        // Verify plan exists
        var plan = await conn.QuerySingleOrDefaultAsync<PlanSummary>(
            new CommandDefinition(
                "SELECT Id, Name, PriceMonthly, PriceYearly, Features, MaxUsers, MaxProducts FROM dbo.Plans WHERE Id = @PlanId AND IsActive = 1",
                new { request.PlanId }, cancellationToken: ct));

        if (plan is null)
            throw new ArgumentException("Invalid or inactive plan.");

        var price = request.BillingPeriod.Equals("Yearly", StringComparison.OrdinalIgnoreCase)
            ? plan.PriceYearly : plan.PriceMonthly;

        // Create PendingSignup record
        var signupId = await conn.QuerySingleAsync<Guid>(
            new CommandDefinition("""
                DECLARE @Ids TABLE(Id UNIQUEIDENTIFIER);
                INSERT INTO dbo.PendingSignups (PharmacyName, Subdomain, AdminEmail, AdminFullName, PlanId, BillingPeriod)
                OUTPUT INSERTED.Id INTO @Ids
                VALUES (@PharmacyName, @Subdomain, @AdminEmail, @AdminFullName, @PlanId, @BillingPeriod);
                SELECT Id FROM @Ids;
                """,
                new
                {
                    request.PharmacyName, request.Subdomain, request.AdminEmail,
                    request.AdminFullName, request.PlanId, request.BillingPeriod
                }, cancellationToken: ct));

        logger.LogInformation("PendingSignup created: {SignupId} for {Subdomain}", signupId, request.Subdomain);

        // Create Stripe Checkout session
        var checkout = await stripeService.CreateSessionAsync(request, signupId, plan.Name, price, ct);

        // Store Stripe session ID
        await conn.ExecuteAsync(
            new CommandDefinition(
                "UPDATE dbo.PendingSignups SET StripeSessionId = @SessionId, UpdatedAt = SYSUTCDATETIME() WHERE Id = @Id",
                new { SessionId = checkout.SessionId, Id = signupId }, cancellationToken: ct));

        return checkout;
    }

    public async Task<SignupStatusResponse> GetStatusAsync(string stripeSessionId, CancellationToken ct = default)
    {
        using var conn = await catalogDb.CreateAsync();

        var signup = await conn.QuerySingleOrDefaultAsync<PendingSignupEntity>(
            new CommandDefinition(
                "SELECT * FROM dbo.PendingSignups WHERE StripeSessionId = @SessionId",
                new { SessionId = stripeSessionId }, cancellationToken: ct));

        if (signup is null)
            return new SignupStatusResponse("not_found", null, null, "Signup session not found.");

        var adminPanelUrl = signup.Status == "active" && signup.Subdomain is not null
            ? $"https://admin.{signup.Subdomain}.zenpharm.com.au"
            : null;

        var message = signup.Status switch
        {
            "pending_payment" => "Waiting for payment confirmation...",
            "provisioning" => "Setting up your pharmacy — this usually takes a minute...",
            "active" => "Your pharmacy is ready!",
            "failed" => signup.FailureReason ?? "Something went wrong. Please contact support.",
            "expired" => "This signup session has expired.",
            _ => null
        };

        return new SignupStatusResponse(signup.Status, signup.TenantId, adminPanelUrl, message);
    }

    public async Task<PendingSignupEntity?> FindByStripeSessionAsync(string sessionId, CancellationToken ct = default)
    {
        using var conn = await catalogDb.CreateAsync();
        return await conn.QuerySingleOrDefaultAsync<PendingSignupEntity>(
            new CommandDefinition(
                "SELECT * FROM dbo.PendingSignups WHERE StripeSessionId = @SessionId",
                new { SessionId = sessionId }, cancellationToken: ct));
    }

    public async Task UpdateStatusAsync(
        Guid signupId, string status, Guid? tenantId = null,
        string? failureReason = null, string? stripeCustomerId = null,
        string? stripeSubscriptionId = null, CancellationToken ct = default)
    {
        using var conn = await catalogDb.CreateAsync();
        await conn.ExecuteAsync(
            new CommandDefinition("""
                UPDATE dbo.PendingSignups
                SET Status = @Status,
                    TenantId = COALESCE(@TenantId, TenantId),
                    FailureReason = COALESCE(@FailureReason, FailureReason),
                    StripeCustomerId = COALESCE(@StripeCustomerId, StripeCustomerId),
                    StripeSubscriptionId = COALESCE(@StripeSubscriptionId, StripeSubscriptionId),
                    UpdatedAt = SYSUTCDATETIME()
                WHERE Id = @Id
                """,
                new { Id = signupId, Status = status, TenantId = tenantId, FailureReason = failureReason, StripeCustomerId = stripeCustomerId, StripeSubscriptionId = stripeSubscriptionId },
                cancellationToken: ct));

        logger.LogInformation("PendingSignup {Id} updated to {Status}", signupId, status);
    }

    public async Task<bool> CheckSubdomainAsync(string subdomain, CancellationToken ct = default)
    {
        if (!SignupValidation.IsValidSubdomain(subdomain))
            return false;

        if (SignupValidation.IsReservedSubdomain(subdomain))
            return false;

        using var conn = await catalogDb.CreateAsync();

        // Check both Tenants AND PendingSignups tables
        var existsInTenants = await conn.ExecuteScalarAsync<int>(
            new CommandDefinition(
                "SELECT COUNT(*) FROM dbo.Tenants WHERE Subdomain = @Subdomain",
                new { Subdomain = subdomain }, cancellationToken: ct));

        if (existsInTenants > 0) return false;

        var existsInPending = await conn.ExecuteScalarAsync<int>(
            new CommandDefinition(
                "SELECT COUNT(*) FROM dbo.PendingSignups WHERE Subdomain = @Subdomain AND Status NOT IN ('failed', 'expired')",
                new { Subdomain = subdomain }, cancellationToken: ct));

        return existsInPending == 0;
    }

    public async Task<IReadOnlyList<PlanSummary>> ListPlansAsync(CancellationToken ct = default)
    {
        using var conn = await catalogDb.CreateAsync();
        var plans = await conn.QueryAsync<PlanSummary>(
            new CommandDefinition(
                "SELECT Id, Name, PriceMonthly, PriceYearly, Features, MaxUsers, MaxProducts FROM dbo.Plans WHERE IsActive = 1 ORDER BY PriceMonthly",
                cancellationToken: ct));
        return plans.ToArray();
    }
}
