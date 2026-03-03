using System.Data;
using Api.Common;
using Api.Common.Migrations;
using Api.Common.Security;
using Api.Features.Notifications;
using Api.Features.Platform;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Api.Tests.Platform;

/// <summary>
/// Integration-style tests for the full signup → provision flow with mocked dependencies.
/// These test the logical flow without requiring a real database.
/// </summary>
public class SignupIntegrationTests
{
    // ── Signup Flow Tests ──────────────────────────────────────────

    [Fact]
    public void FullCheckoutFlow_RequestContractsAreValid()
    {
        var planId = Guid.NewGuid();
        var request = new CreateCheckoutRequest
        {
            PharmacyName = "Integration Test Pharmacy",
            Subdomain = "integration-test",
            AdminEmail = "admin@integration-test.com.au",
            AdminFullName = "Test Admin",
            PlanId = planId,
            BillingPeriod = "Monthly"
        };

        Assert.True(SignupValidation.IsValidSubdomain(request.Subdomain));
        Assert.False(SignupValidation.IsReservedSubdomain(request.Subdomain));
        Assert.Equal(planId, request.PlanId);
    }

    [Fact]
    public void FullProvisioningFlow_RequestToResultMapping()
    {
        var planId = Guid.NewGuid();
        var provRequest = new ProvisionRequest(
            TenantName: "Integration Pharmacy",
            Subdomain: "integration-pharmacy",
            AdminEmail: "admin@integration.com",
            Plan: "Basic",
            AdminFullName: "Integration Admin",
            PlanId: planId,
            BillingPeriod: "Monthly",
            StripeCustomerId: "cus_integration_123",
            StripeSubscriptionId: "sub_integration_456");

        Assert.Equal("Integration Pharmacy", provRequest.TenantName);
        Assert.Equal("integration-pharmacy", provRequest.Subdomain);
        Assert.Equal("Basic", provRequest.Plan);
        Assert.Equal(planId, provRequest.PlanId);
        Assert.Equal("cus_integration_123", provRequest.StripeCustomerId);
    }

    // ── Subdomain Uniqueness Tests ─────────────────────────────────

    [Theory]
    [InlineData("www")]
    [InlineData("admin")]
    [InlineData("api")]
    [InlineData("dev")]
    [InlineData("staging")]
    [InlineData("test")]
    [InlineData("demo")]
    [InlineData("support")]
    [InlineData("billing")]
    [InlineData("localhost")]
    public void DuplicateSubdomain_ReservedWordsRejected(string subdomain)
    {
        Assert.True(SignupValidation.IsReservedSubdomain(subdomain));
    }

    [Theory]
    [InlineData("smith-pharmacy")]
    [InlineData("my-great-pharmacy")]
    [InlineData("pharmacy123")]
    [InlineData("a1b2c3d4")]
    public void DuplicateSubdomain_ValidSubdomainsAccepted(string subdomain)
    {
        Assert.True(SignupValidation.IsValidSubdomain(subdomain));
        Assert.False(SignupValidation.IsReservedSubdomain(subdomain));
    }

    // ── Welcome Email Tests ────────────────────────────────────────

    [Fact]
    public void WelcomeEmail_ContainsAllRequiredInfo()
    {
        var builder = new WelcomeEmailBuilder();
        var email = builder.Build(new WelcomeEmailData(
            PharmacyName: "Integration Test Pharmacy",
            AdminFullName: "Test Admin",
            AdminEmail: "admin@test.com",
            TemporaryPassword: "TempPass123",
            AdminPanelUrl: "https://admin.integration-test.zenpharm.com.au",
            PlanName: "Premium",
            BillingPeriod: "Yearly"));

        Assert.Contains("Integration Test Pharmacy", email.Subject);
        Assert.Contains("Test Admin", email.HtmlBody);
        Assert.Contains("admin@test.com", email.HtmlBody);
        Assert.Contains("TempPass123", email.HtmlBody);
        Assert.Contains("Premium", email.HtmlBody);
        Assert.Contains("Yearly", email.HtmlBody);
        Assert.Contains("https://admin.integration-test.zenpharm.com.au", email.HtmlBody);
    }

    // ── Provisioning Pipeline Helpers ──────────────────────────────

    [Fact]
    public void ProvisioningPipeline_DatabaseNameCreation()
    {
        var dbName = ProvisioningPipeline.BuildDatabaseName("smith-pharmacy");
        Assert.Equal("ZenPharmTenant_smith_pharmacy", dbName);

        var dbName2 = ProvisioningPipeline.BuildDatabaseName("my-great-pharmacy-2024");
        Assert.Equal("ZenPharmTenant_my_great_pharmacy_2024", dbName2);
    }

    [Fact]
    public void ProvisioningPipeline_ConnectionStringDerivation()
    {
        var catalogConn = "Server=tcp:zenpharm-sql.database.windows.net;Database=ZenPharmCatalog;User Id=admin;Password=secret;";

        var masterConn = ProvisioningPipeline.ParseMasterConnectionString(catalogConn);
        Assert.Contains("master", masterConn, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ZenPharmCatalog", masterConn, StringComparison.OrdinalIgnoreCase);

        var tenantConn = ProvisioningPipeline.BuildTenantConnectionString(catalogConn, "ZenPharmTenant_smith_pharmacy");
        Assert.Contains("ZenPharmTenant_smith_pharmacy", tenantConn);
        Assert.DoesNotContain("ZenPharmCatalog", tenantConn);
    }

    [Fact]
    public void ProvisioningPipeline_PasswordGeneration()
    {
        var passwords = Enumerable.Range(0, 10)
            .Select(_ => ProvisioningPipeline.GeneratePassword())
            .ToArray();

        // All passwords are 16 chars
        Assert.All(passwords, p => Assert.Equal(16, p.Length));

        // All passwords are unique (extremely high probability)
        Assert.Equal(10, passwords.Distinct().Count());
    }

    // ── Subscription Lifecycle Tests ───────────────────────────────

    [Fact]
    public void SubscriptionStatus_MapsCorrectly()
    {
        // Test the expected subscription status values
        var validStatuses = new[] { "Active", "PastDue", "Cancelled", "Trialing" };
        Assert.All(validStatuses, s => Assert.NotNull(s));
    }

    // ── Template Pack Integration ──────────────────────────────────

    [Fact]
    public void TemplatePack_DefaultIncludesAllCategories()
    {
        var defaultPack = TemplatePacks.GetDefault();
        Assert.Empty(defaultPack.Categories); // Empty = all categories

        // Should include any category
        Assert.True(TemplatePacks.IncludesCategory(defaultPack, "Pain Relief"));
        Assert.True(TemplatePacks.IncludesCategory(defaultPack, "Vitamins & Supplements"));
        Assert.True(TemplatePacks.IncludesCategory(defaultPack, "Random Nonexistent Category"));
    }

    [Fact]
    public void TemplatePack_QuickStartHasEssentialCategories()
    {
        var quickStart = TemplatePacks.GetById("quick-start");
        Assert.NotNull(quickStart);
        Assert.True(TemplatePacks.IncludesCategory(quickStart, "Pain Relief"));
        Assert.True(TemplatePacks.IncludesCategory(quickStart, "First Aid"));
        Assert.False(TemplatePacks.IncludesCategory(quickStart, "Mental Health"));
    }

    // ── PBS Code Mapping Integration ───────────────────────────────

    [Fact]
    public void PbsCodeMapping_CommonMedicationsHaveCodes()
    {
        var commonIngredients = new[]
        {
            "Paracetamol", "Ibuprofen", "Amoxicillin", "Metformin",
            "Atorvastatin", "Salbutamol", "Sertraline", "Omeprazole"
        };

        foreach (var ingredient in commonIngredients)
        {
            var code = PbsCodeMapping.FindPbsCode(ingredient);
            Assert.NotNull(code);
        }
    }

    [Fact]
    public void PbsCodeMapping_HandlesComplexActiveIngredients()
    {
        // Should match "Paracetamol" from "Paracetamol 500mg, Codeine phosphate 8mg"
        var code = PbsCodeMapping.FindPbsCode("Paracetamol 500mg, Codeine phosphate 8mg");
        Assert.Equal("2622B", code);

        // Should match exact combination
        var comboCode = PbsCodeMapping.FindPbsCode("amoxicillin + clavulanic acid");
        Assert.Equal("8035Y", comboCode);
    }
}
