using Stripe;
using Stripe.Checkout;

namespace Api.Features.Platform;

internal sealed class StripeCheckoutService(
    IConfiguration configuration,
    ILogger<StripeCheckoutService> logger) : IStripeCheckoutService
{
    public async Task<CheckoutResponse> CreateSessionAsync(
        CreateCheckoutRequest request,
        Guid signupId,
        string planName,
        decimal price,
        CancellationToken ct = default)
    {
        var secretKey = configuration["Stripe:SecretKey"]
            ?? throw new InvalidOperationException("Stripe:SecretKey is not configured.");

        StripeConfiguration.ApiKey = secretKey;

        var successUrl = configuration["Stripe:SuccessUrl"]
            ?? "http://localhost:51000/signup/success?session_id={CHECKOUT_SESSION_ID}";
        var cancelUrl = configuration["Stripe:CancelUrl"]
            ?? "http://localhost:51000/pricing";

        var interval = request.BillingPeriod.Equals("Yearly", StringComparison.OrdinalIgnoreCase)
            ? "year" : "month";

        var options = new SessionCreateOptions
        {
            Mode = "subscription",
            CustomerEmail = request.AdminEmail,
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
            Metadata = new Dictionary<string, string>
            {
                ["signupId"] = signupId.ToString(),
                ["subdomain"] = request.Subdomain,
                ["pharmacyName"] = request.PharmacyName
            },
            LineItems =
            [
                new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "aud",
                        UnitAmount = (long)(price * 100),
                        Recurring = new SessionLineItemPriceDataRecurringOptions
                        {
                            Interval = interval
                        },
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = $"ZenPharm {planName} Plan",
                            Description = $"{planName} plan — {request.BillingPeriod} billing"
                        }
                    },
                    Quantity = 1
                }
            ]
        };

        var service = new SessionService();
        var session = await service.CreateAsync(options, cancellationToken: ct);

        logger.LogInformation(
            "Stripe Checkout session created: {SessionId} for signup {SignupId}",
            session.Id, signupId);

        return new CheckoutResponse(session.Url, session.Id);
    }
}
