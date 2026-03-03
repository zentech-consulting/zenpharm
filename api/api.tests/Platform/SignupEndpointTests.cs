using Api.Features.Platform;
using Xunit;

namespace Api.Tests.Platform;

public class SignupEndpointTests
{
    [Fact]
    public void CreateCheckoutRequest_DefaultBillingPeriod_IsMonthly()
    {
        var req = new CreateCheckoutRequest
        {
            PharmacyName = "Test",
            Subdomain = "test-pharmacy",
            AdminEmail = "a@b.com",
            AdminFullName = "Test User",
            PlanId = Guid.NewGuid()
        };
        Assert.Equal("Monthly", req.BillingPeriod);
    }

    [Fact]
    public void CreateCheckoutRequest_YearlyBillingPeriod()
    {
        var req = new CreateCheckoutRequest
        {
            PharmacyName = "Test",
            Subdomain = "test-pharmacy",
            AdminEmail = "a@b.com",
            AdminFullName = "Test User",
            PlanId = Guid.NewGuid(),
            BillingPeriod = "Yearly"
        };
        Assert.Equal("Yearly", req.BillingPeriod);
    }

    [Fact]
    public void SignupStatusResponse_NotFound()
    {
        var resp = new SignupStatusResponse("not_found", null, null, "Signup session not found.");
        Assert.Equal("not_found", resp.Status);
        Assert.Null(resp.TenantId);
    }

    [Fact]
    public void SignupStatusResponse_Provisioning()
    {
        var resp = new SignupStatusResponse("provisioning", null, null, "Setting up your pharmacy...");
        Assert.Equal("provisioning", resp.Status);
    }

    [Theory]
    [InlineData("Monthly")]
    [InlineData("Yearly")]
    public void CreateCheckoutRequest_ValidBillingPeriods(string period)
    {
        var req = new CreateCheckoutRequest
        {
            PharmacyName = "Test",
            Subdomain = "test-pharmacy",
            AdminEmail = "a@b.com",
            AdminFullName = "Test User",
            PlanId = Guid.NewGuid(),
            BillingPeriod = period
        };
        Assert.Equal(period, req.BillingPeriod);
    }
}
