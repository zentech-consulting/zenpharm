using Api.Features.Platform;
using Xunit;

namespace Api.Tests.Platform;

public class SignupContractTests
{
    [Fact]
    public void CreateCheckoutRequest_RecordEquality()
    {
        var planId = Guid.NewGuid();
        var a = new CreateCheckoutRequest
        {
            PharmacyName = "Smith Pharmacy",
            Subdomain = "smith-pharmacy",
            AdminEmail = "admin@smith.com",
            AdminFullName = "John Smith",
            PlanId = planId
        };
        var b = new CreateCheckoutRequest
        {
            PharmacyName = "Smith Pharmacy",
            Subdomain = "smith-pharmacy",
            AdminEmail = "admin@smith.com",
            AdminFullName = "John Smith",
            PlanId = planId
        };

        Assert.Equal(a, b);
    }

    [Fact]
    public void CheckoutResponse_Properties()
    {
        var resp = new CheckoutResponse("https://checkout.stripe.com/session/cs_test_123", "cs_test_123");

        Assert.Equal("https://checkout.stripe.com/session/cs_test_123", resp.CheckoutUrl);
        Assert.Equal("cs_test_123", resp.SessionId);
    }

    [Fact]
    public void SignupStatusResponse_ActiveStatus()
    {
        var tenantId = Guid.NewGuid();
        var resp = new SignupStatusResponse("active", tenantId, "https://admin.smith-pharmacy.zenpharm.com.au", "Your pharmacy is ready!");

        Assert.Equal("active", resp.Status);
        Assert.Equal(tenantId, resp.TenantId);
        Assert.NotNull(resp.AdminPanelUrl);
        Assert.NotNull(resp.Message);
    }

    [Fact]
    public void SignupStatusResponse_PendingStatus()
    {
        var resp = new SignupStatusResponse("pending_payment", null, null, null);

        Assert.Equal("pending_payment", resp.Status);
        Assert.Null(resp.TenantId);
    }

    [Fact]
    public void PlanSummary_Properties()
    {
        var id = Guid.NewGuid();
        var plan = new PlanSummary(id, "Basic", 79.00m, 790.00m, """{"products":500}""", 5, 500);

        Assert.Equal(id, plan.Id);
        Assert.Equal("Basic", plan.Name);
        Assert.Equal(79.00m, plan.PriceMonthly);
        Assert.Equal(790.00m, plan.PriceYearly);
    }

    [Fact]
    public void PendingSignupEntity_DefaultValues()
    {
        var entity = new PendingSignupEntity();

        Assert.Equal(Guid.Empty, entity.Id);
        Assert.Equal("", entity.PharmacyName);
        Assert.Equal("Monthly", entity.BillingPeriod);
        Assert.Equal("pending_payment", entity.Status);
        Assert.Null(entity.StripeSessionId);
        Assert.Null(entity.TenantId);
    }

    [Theory]
    [InlineData("smith-pharmacy", true)]
    [InlineData("abc", true)]
    [InlineData("my-pharmacy-123", true)]
    [InlineData("a1b2c3", true)]
    [InlineData("ab", false)]       // too short
    [InlineData("", false)]          // empty
    [InlineData("-invalid", false)]  // starts with hyphen
    [InlineData("invalid-", false)]  // ends with hyphen
    [InlineData("UPPERCASE", false)] // uppercase not allowed
    [InlineData("has space", false)]
    [InlineData("has.dot", false)]
    public void SignupValidation_IsValidSubdomain(string subdomain, bool expected)
    {
        Assert.Equal(expected, SignupValidation.IsValidSubdomain(subdomain));
    }

    [Theory]
    [InlineData("www", true)]
    [InlineData("api", true)]
    [InlineData("admin", true)]
    [InlineData("dev", true)]
    [InlineData("staging", true)]
    [InlineData("test", true)]
    [InlineData("demo", true)]
    [InlineData("support", true)]
    [InlineData("localhost", true)]
    [InlineData("smith-pharmacy", false)]
    [InlineData("my-awesome-store", false)]
    public void SignupValidation_IsReservedSubdomain(string subdomain, bool expected)
    {
        Assert.Equal(expected, SignupValidation.IsReservedSubdomain(subdomain));
    }
}
