using Api.Features.Platform;
using Xunit;

namespace Api.Tests.Platform;

public class ProvisioningPipelineTests
{
    [Theory]
    [InlineData("Server=tcp:zenpharm-sql.database.windows.net;Database=ZenPharmCatalog;User Id=admin;Password=pass;")]
    [InlineData("Data Source=localhost;Initial Catalog=ZenPharmCatalog;Integrated Security=true;")]
    public void ParseMasterConnectionString_ReplacesDatabaseWithMaster(string input)
    {
        var result = ProvisioningPipeline.ParseMasterConnectionString(input);
        Assert.Contains("master", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ZenPharmCatalog", result, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("smith-pharmacy", "ZenPharmTenant_smith_pharmacy")]
    [InlineData("demo", "ZenPharmTenant_demo")]
    [InlineData("abc-123-xyz", "ZenPharmTenant_abc_123_xyz")]
    public void BuildDatabaseName_FormatsCorrectly(string subdomain, string expected)
    {
        var result = ProvisioningPipeline.BuildDatabaseName(subdomain);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void BuildTenantConnectionString_SetsCorrectDatabase()
    {
        var catalogConn = "Server=tcp:zenpharm-sql.database.windows.net;Database=ZenPharmCatalog;User Id=admin;Password=pass;";
        var dbName = "ZenPharmTenant_smith";

        var result = ProvisioningPipeline.BuildTenantConnectionString(catalogConn, dbName);
        Assert.Contains("ZenPharmTenant_smith", result);
        Assert.DoesNotContain("ZenPharmCatalog", result);
    }

    [Fact]
    public void GeneratePassword_Returns16Characters()
    {
        var password = ProvisioningPipeline.GeneratePassword();
        Assert.Equal(16, password.Length);
    }

    [Fact]
    public void GeneratePassword_ContainsOnlyAllowedCharacters()
    {
        const string allowed = "abcdefghijkmnpqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var password = ProvisioningPipeline.GeneratePassword();
        foreach (var c in password)
            Assert.Contains(c, allowed);
    }

    [Fact]
    public void GeneratePassword_ProducesDifferentValues()
    {
        var p1 = ProvisioningPipeline.GeneratePassword();
        var p2 = ProvisioningPipeline.GeneratePassword();
        // Extremely unlikely to be the same with 16 crypto-random chars
        Assert.NotEqual(p1, p2);
    }

    [Fact]
    public void ProvisionRequest_RecordEquality_WithNewFields()
    {
        var planId = Guid.NewGuid();
        var a = new ProvisionRequest("Acme", "acme", "admin@acme.com", "Basic",
            AdminFullName: "John Doe", PlanId: planId, BillingPeriod: "Monthly",
            StripeCustomerId: "cus_123", StripeSubscriptionId: "sub_456");
        var b = new ProvisionRequest("Acme", "acme", "admin@acme.com", "Basic",
            AdminFullName: "John Doe", PlanId: planId, BillingPeriod: "Monthly",
            StripeCustomerId: "cus_123", StripeSubscriptionId: "sub_456");

        Assert.Equal(a, b);
    }

    [Fact]
    public void ProvisionRequest_BackwardCompatible()
    {
        // Old-style ProvisionRequest should still work
        var req = new ProvisionRequest("Acme", "acme", "admin@acme.com", "pro");
        Assert.Equal("Acme", req.TenantName);
        Assert.Null(req.AdminFullName);
        Assert.Null(req.PlanId);
    }

    [Fact]
    public void ProvisionResult_IncludesAdminPassword()
    {
        var result = new ProvisionResult(true, "tid-123", "OK", AdminPassword: "pass123");
        Assert.True(result.Success);
        Assert.Equal("pass123", result.AdminPassword);
    }
}
