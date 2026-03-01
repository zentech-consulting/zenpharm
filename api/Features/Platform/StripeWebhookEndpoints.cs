namespace Api.Features.Platform;

public static class StripeWebhookEndpoints
{
    public static void MapStripeWebhookEndpoints(this WebApplication app)
    {
        app.MapPost("/api/webhooks/stripe", async (
            HttpContext context,
            ILogger<ProvisioningPipeline> logger,
            CancellationToken ct) =>
        {
            // In production: verify Stripe signature from headers
            // var signature = context.Request.Headers["Stripe-Signature"].FirstOrDefault();

            StripeWebhookEvent? evt;
            try
            {
                evt = await context.Request.ReadFromJsonAsync<StripeWebhookEvent>(ct);
            }
            catch
            {
                return Results.BadRequest(new { error = "Invalid webhook payload" });
            }

            if (evt is null)
                return Results.BadRequest(new { error = "Empty webhook payload" });

            logger.LogInformation("Received Stripe webhook: {Type} ({EventId})", evt.Type, evt.Id);

            // Stub handlers — log and acknowledge
            switch (evt.Type)
            {
                case "checkout.session.completed":
                    logger.LogInformation("Checkout completed for customer {Customer}",
                        evt.Data.Object.Customer);
                    break;

                case "customer.subscription.updated":
                    logger.LogInformation("Subscription updated: {Status}",
                        evt.Data.Object.Status);
                    break;

                case "customer.subscription.deleted":
                    logger.LogInformation("Subscription deleted for customer {Customer}",
                        evt.Data.Object.Customer);
                    break;

                default:
                    logger.LogDebug("Unhandled Stripe event type: {Type}", evt.Type);
                    break;
            }

            return Results.Ok(new { received = true });
        })
        .WithTags("Webhooks")
        .AllowAnonymous()
        .WithOpenApi(op => { op.Summary = "Stripe webhook receiver"; return op; });
    }
}
