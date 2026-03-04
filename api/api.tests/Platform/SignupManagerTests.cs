using System.Data;
using Api.Common;
using Api.Features.Platform;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Api.Tests.Platform;

public class SignupManagerTests
{
    private readonly ICatalogDb _catalogDb = Substitute.For<ICatalogDb>();
    private readonly IStripeCheckoutService _stripeService = Substitute.For<IStripeCheckoutService>();
    private readonly ILogger<SignupManager> _logger = Substitute.For<ILogger<SignupManager>>();
    private readonly SignupManager _sut;

    public SignupManagerTests()
    {
        _sut = new SignupManager(_catalogDb, _stripeService, _logger);
    }

    [Theory]
    [InlineData("ab")]        // too short
    [InlineData("-invalid")]  // starts with hyphen
    [InlineData("UPPER")]     // uppercase
    public async Task CreateCheckoutAsync_InvalidSubdomain_Throws(string subdomain)
    {
        var request = MakeRequest(subdomain: subdomain);

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => _sut.CreateCheckoutAsync(request));
        Assert.Contains("subdomain", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("www")]
    [InlineData("admin")]
    [InlineData("dev")]
    [InlineData("staging")]
    [InlineData("test")]
    public async Task CreateCheckoutAsync_ReservedSubdomain_Throws(string subdomain)
    {
        var request = MakeRequest(subdomain: subdomain);

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => _sut.CreateCheckoutAsync(request));
        Assert.Contains("reserved", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateCheckoutAsync_CatalogDbUnavailable_Throws()
    {
        _catalogDb.CreateAsync().Returns<IDbConnection>(
            _ => throw new InvalidOperationException("DB unavailable"));

        var request = MakeRequest();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.CreateCheckoutAsync(request));
    }

    [Fact]
    public void SignupStatusResponse_PendingPayment_HasMessage()
    {
        var response = new SignupStatusResponse("pending_payment", null, null, "Waiting for payment confirmation...");
        Assert.Equal("pending_payment", response.Status);
        Assert.Null(response.TenantId);
        Assert.Contains("payment", response.Message!);
    }

    [Fact]
    public void SignupStatusResponse_Active_HasAdminPanelUrl()
    {
        var tenantId = Guid.NewGuid();
        var response = new SignupStatusResponse("active", tenantId, "https://admin.smith.zenpharm.com.au", "Your pharmacy is ready!");
        Assert.Equal("active", response.Status);
        Assert.NotNull(response.TenantId);
        Assert.NotNull(response.AdminPanelUrl);
    }

    [Fact]
    public void SignupStatusResponse_Failed_HasFailureReason()
    {
        var response = new SignupStatusResponse("failed", null, null, "Database creation failed");
        Assert.Equal("failed", response.Status);
        Assert.Contains("failed", response.Message!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CheckSubdomainAsync_InvalidFormat_ReturnsFalse()
    {
        // This tests the synchronous validation path before DB check
        Assert.False(SignupValidation.IsValidSubdomain("ab"));
        Assert.False(SignupValidation.IsValidSubdomain("-bad"));
        Assert.True(SignupValidation.IsValidSubdomain("good-subdomain"));
    }

    [Fact]
    public void CheckSubdomainAsync_Reserved_ReturnsFalse()
    {
        Assert.True(SignupValidation.IsReservedSubdomain("www"));
        Assert.True(SignupValidation.IsReservedSubdomain("admin"));
        Assert.True(SignupValidation.IsReservedSubdomain("localhost"));
        Assert.False(SignupValidation.IsReservedSubdomain("smith-pharmacy"));
    }

    private static CreateCheckoutRequest MakeRequest(
        string subdomain = "smith-pharmacy",
        string pharmacyName = "Smith Pharmacy",
        string adminEmail = "admin@smith.com",
        string adminFullName = "John Smith",
        string billingPeriod = "Monthly")
    {
        return new CreateCheckoutRequest
        {
            PharmacyName = pharmacyName,
            Subdomain = subdomain,
            AdminEmail = adminEmail,
            AdminFullName = adminFullName,
            PlanId = Guid.NewGuid(),
            BillingPeriod = billingPeriod
        };
    }
}
