using Api.Features.Platform;
using Xunit;

namespace Api.Tests.Platform;

public class StripeWebhookTests
{
    [Fact]
    public void StripeWebhookEvent_LegacyContract_DefaultValues()
    {
        var evt = new StripeWebhookEvent();
        Assert.Equal("", evt.Id);
        Assert.Equal("", evt.Type);
        Assert.NotNull(evt.Data);
        Assert.NotNull(evt.Data.Object);
    }

    [Fact]
    public void StripeWebhookEvent_LegacyContract_SetValues()
    {
        var evt = new StripeWebhookEvent
        {
            Id = "evt_test_123",
            Type = "checkout.session.completed",
            Data = new StripeEventData
            {
                Object = new StripeEventObject
                {
                    Id = "cs_test_456",
                    Customer = "cus_789",
                    Status = "complete"
                }
            }
        };

        Assert.Equal("evt_test_123", evt.Id);
        Assert.Equal("checkout.session.completed", evt.Type);
        Assert.Equal("cus_789", evt.Data.Object.Customer);
        Assert.Equal("complete", evt.Data.Object.Status);
    }

    [Fact]
    public void StripeEventTypes_CheckoutSessionCompleted_IsExpectedString()
    {
        // Verify the Stripe.net SDK event type constant matches expected value
        Assert.Equal("checkout.session.completed", Stripe.EventTypes.CheckoutSessionCompleted);
    }

    [Fact]
    public void StripeEventTypes_SubscriptionUpdated_IsExpectedString()
    {
        Assert.Equal("customer.subscription.updated", Stripe.EventTypes.CustomerSubscriptionUpdated);
    }

    [Fact]
    public void StripeEventTypes_SubscriptionDeleted_IsExpectedString()
    {
        Assert.Equal("customer.subscription.deleted", Stripe.EventTypes.CustomerSubscriptionDeleted);
    }

    [Fact]
    public void TenantMiddleware_BypassPrefixes_IncludesWebhooks()
    {
        // Verify that webhook and signup paths bypass tenant resolution
        var field = typeof(Api.Common.Tenancy.TenantMiddleware)
            .GetField("BypassPrefixes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Assert.NotNull(field);

        var prefixes = (string[])field.GetValue(null)!;
        Assert.Contains("/api/webhooks", prefixes);
        Assert.Contains("/api/signup", prefixes);
    }
}
