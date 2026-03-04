using Api.Common;
using Api.Features.Notifications;
using Dapper;
using Stripe;

namespace Api.Features.Platform;

public static class StripeWebhookEndpoints
{
    public static void MapStripeWebhookEndpoints(this WebApplication app)
    {
        app.MapPost("/api/webhooks/stripe", async (
            HttpContext context,
            IConfiguration cfg,
            ISignupManager signupManager,
            IProvisioningPipeline pipeline,
            IWelcomeEmailBuilder emailBuilder,
            IEmailService emailService,
            ICatalogDb catalogDb,
            ILogger<ProvisioningPipeline> logger,
            CancellationToken ct) =>
        {
            // 1. Read raw body for signature verification
            context.Request.EnableBuffering();
            string body;
            using (var reader = new StreamReader(context.Request.Body, leaveOpen: true))
            {
                body = await reader.ReadToEndAsync(ct);
            }

            // 2. Verify Stripe signature
            var webhookSecret = cfg["Stripe:WebhookSecret"];
            Event stripeEvent;

            if (!string.IsNullOrEmpty(webhookSecret))
            {
                var signature = context.Request.Headers["Stripe-Signature"].FirstOrDefault();
                if (string.IsNullOrEmpty(signature))
                {
                    logger.LogWarning("Stripe webhook rejected — missing Stripe-Signature header");
                    return Results.Unauthorized();
                }

                try
                {
                    stripeEvent = EventUtility.ConstructEvent(body, signature, webhookSecret);
                }
                catch (StripeException ex)
                {
                    logger.LogWarning(ex, "Stripe webhook signature verification failed");
                    return Results.BadRequest(new { error = "Invalid signature" });
                }
            }
            else
            {
                // Development fallback — accept unverified payloads
                if (!context.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
                {
                    logger.LogError("Stripe webhook rejected — Stripe:WebhookSecret not configured in production");
                    return Results.StatusCode(503);
                }
                logger.LogWarning("Stripe webhook secret not configured — accepting unverified payloads in Development only");

                try
                {
                    stripeEvent = EventUtility.ParseEvent(body);
                }
                catch (StripeException)
                {
                    return Results.BadRequest(new { error = "Invalid webhook payload" });
                }
            }

            logger.LogInformation("Received Stripe webhook: {Type} ({EventId})", stripeEvent.Type, stripeEvent.Id);

            switch (stripeEvent.Type)
            {
                case EventTypes.CheckoutSessionCompleted:
                    await HandleCheckoutCompletedAsync(
                        stripeEvent, signupManager, pipeline, emailBuilder, emailService, catalogDb, logger, ct);
                    break;

                case EventTypes.CustomerSubscriptionUpdated:
                    await HandleSubscriptionUpdatedAsync(stripeEvent, catalogDb, logger, ct);
                    break;

                case EventTypes.CustomerSubscriptionDeleted:
                    await HandleSubscriptionDeletedAsync(stripeEvent, catalogDb, logger, ct);
                    break;

                default:
                    logger.LogDebug("Unhandled Stripe event type: {Type}", stripeEvent.Type);
                    break;
            }

            return Results.Ok(new { received = true });
        })
        .WithTags("Webhooks")
        .AllowAnonymous()
        .WithOpenApi(op => { op.Summary = "Stripe webhook receiver"; return op; });
    }

    private static async Task HandleCheckoutCompletedAsync(
        Event stripeEvent,
        ISignupManager signupManager,
        IProvisioningPipeline pipeline,
        IWelcomeEmailBuilder emailBuilder,
        IEmailService emailService,
        ICatalogDb catalogDb,
        ILogger logger,
        CancellationToken ct)
    {
        var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
        if (session is null)
        {
            logger.LogWarning("checkout.session.completed event has no session object");
            return;
        }

        // Find PendingSignup by signupId in metadata
        var signupIdStr = session.Metadata?.GetValueOrDefault("signupId");
        if (string.IsNullOrEmpty(signupIdStr) || !Guid.TryParse(signupIdStr, out var signupId))
        {
            logger.LogWarning("checkout.session.completed missing signupId metadata");
            return;
        }

        // Find signup by session ID
        var signup = await signupManager.FindByStripeSessionAsync(session.Id, ct);
        if (signup is null)
        {
            logger.LogWarning("PendingSignup not found for session {SessionId}", session.Id);
            return;
        }

        // Mark as provisioning
        await signupManager.UpdateStatusAsync(signupId, "provisioning",
            stripeCustomerId: session.CustomerId,
            stripeSubscriptionId: session.SubscriptionId, ct: ct);

        // Get plan info
        using var conn = await catalogDb.CreateAsync();
        var plan = await conn.QuerySingleOrDefaultAsync<PlanSummary>(
            new CommandDefinition(
                "SELECT Id, Name, PriceMonthly, PriceYearly, Features, MaxUsers, MaxProducts FROM dbo.Plans WHERE Id = @PlanId",
                new { signup.PlanId }, cancellationToken: ct));

        var planName = plan?.Name ?? "Basic";

        // Run provisioning pipeline
        var result = await pipeline.ProvisionAsync(new ProvisionRequest(
            TenantName: signup.PharmacyName,
            Subdomain: signup.Subdomain,
            AdminEmail: signup.AdminEmail,
            Plan: planName,
            AdminFullName: signup.AdminFullName,
            PlanId: signup.PlanId,
            BillingPeriod: signup.BillingPeriod,
            StripeCustomerId: session.CustomerId,
            StripeSubscriptionId: session.SubscriptionId), ct);

        if (result.Success && result.TenantId is not null)
        {
            await signupManager.UpdateStatusAsync(signupId, "active",
                tenantId: Guid.Parse(result.TenantId), ct: ct);

            // Send welcome email
            var adminPanelUrl = $"https://admin.{signup.Subdomain}.zenpharm.com.au";
            var welcomeEmail = emailBuilder.Build(new WelcomeEmailData(
                PharmacyName: signup.PharmacyName,
                AdminFullName: signup.AdminFullName,
                AdminEmail: signup.AdminEmail,
                TemporaryPassword: result.AdminPassword ?? "See admin panel",
                AdminPanelUrl: adminPanelUrl,
                PlanName: planName,
                BillingPeriod: signup.BillingPeriod));

            await emailService.SendAsync(signup.AdminEmail, welcomeEmail.Subject, welcomeEmail.HtmlBody, ct);

            logger.LogInformation("Tenant {TenantId} provisioned and welcome email sent for {Subdomain}",
                result.TenantId, signup.Subdomain);
        }
        else
        {
            await signupManager.UpdateStatusAsync(signupId, "failed",
                failureReason: result.Message, ct: ct);

            logger.LogError("Provisioning failed for signup {SignupId}: {Message}", signupId, result.Message);
        }
    }

    private static async Task HandleSubscriptionUpdatedAsync(
        Event stripeEvent, ICatalogDb catalogDb, ILogger logger, CancellationToken ct)
    {
        var subscription = stripeEvent.Data.Object as Stripe.Subscription;
        if (subscription is null) return;

        using var conn = await catalogDb.CreateAsync();

        var status = subscription.Status switch
        {
            "active" => "Active",
            "past_due" => "PastDue",
            "canceled" or "cancelled" => "Cancelled",
            "trialing" => "Trialing",
            _ => "Active"
        };

        await conn.ExecuteAsync(
            new CommandDefinition("""
                UPDATE dbo.Subscriptions
                SET Status = @Status,
                    CurrentPeriodStart = @PeriodStart,
                    CurrentPeriodEnd = @PeriodEnd,
                    UpdatedAt = SYSUTCDATETIME()
                WHERE StripeSubscriptionId = @SubId
                """,
                new
                {
                    Status = status,
                    PeriodStart = subscription.CurrentPeriodStart,
                    PeriodEnd = subscription.CurrentPeriodEnd,
                    SubId = subscription.Id
                }, cancellationToken: ct));

        logger.LogInformation("Subscription {SubId} updated to {Status}", subscription.Id, status);
    }

    private static async Task HandleSubscriptionDeletedAsync(
        Event stripeEvent, ICatalogDb catalogDb, ILogger logger, CancellationToken ct)
    {
        var subscription = stripeEvent.Data.Object as Stripe.Subscription;
        if (subscription is null) return;

        using var conn = await catalogDb.CreateAsync();

        // Suspend the tenant when subscription is deleted
        await conn.ExecuteAsync(
            new CommandDefinition("""
                UPDATE dbo.Subscriptions SET Status = 'Cancelled', UpdatedAt = SYSUTCDATETIME()
                WHERE StripeSubscriptionId = @SubId;

                UPDATE dbo.Tenants SET Status = 'Suspended', UpdatedAt = SYSUTCDATETIME()
                WHERE Id IN (
                    SELECT TenantId FROM dbo.Subscriptions WHERE StripeSubscriptionId = @SubId
                );
                """,
                new { SubId = subscription.Id }, cancellationToken: ct));

        logger.LogInformation("Subscription {SubId} deleted — tenant suspended", subscription.Id);
    }
}
