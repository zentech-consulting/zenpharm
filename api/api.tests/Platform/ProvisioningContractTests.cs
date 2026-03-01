using Api.Features.Platform;
using Xunit;

namespace Api.Tests.Platform;

public class ProvisioningContractTests
{
    [Fact]
    public void ProvisionRequest_RecordEquality()
    {
        var a = new ProvisionRequest("Acme", "acme", "admin@acme.com", "pro");
        var b = new ProvisionRequest("Acme", "acme", "admin@acme.com", "pro");

        Assert.Equal(a, b);
    }

    [Fact]
    public void ProvisionResult_SuccessResult()
    {
        var result = new ProvisionResult(true, "tenant-123", "Provisioned");

        Assert.True(result.Success);
        Assert.Equal("tenant-123", result.TenantId);
        Assert.Equal("Provisioned", result.Message);
    }

    [Fact]
    public void ProvisionResult_FailureResult()
    {
        var result = new ProvisionResult(false, null, "Subdomain already taken");

        Assert.False(result.Success);
        Assert.Null(result.TenantId);
    }

    [Fact]
    public void StripeWebhookEvent_DefaultValues()
    {
        var evt = new StripeWebhookEvent();

        Assert.Equal("", evt.Id);
        Assert.Equal("", evt.Type);
        Assert.NotNull(evt.Data);
        Assert.NotNull(evt.Data.Object);
        Assert.Equal("", evt.Data.Object.Id);
        Assert.Null(evt.Data.Object.Customer);
        Assert.Null(evt.Data.Object.Status);
    }
}
