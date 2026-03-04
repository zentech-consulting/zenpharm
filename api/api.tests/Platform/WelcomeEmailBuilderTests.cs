using Api.Features.Platform;
using Xunit;

namespace Api.Tests.Platform;

public class WelcomeEmailBuilderTests
{
    private readonly IWelcomeEmailBuilder _builder = new WelcomeEmailBuilder();

    private static WelcomeEmailData CreateTestData() => new(
        PharmacyName: "Smith Pharmacy",
        AdminFullName: "John Smith",
        AdminEmail: "john@smithpharmacy.com.au",
        TemporaryPassword: "TempPass123!",
        AdminPanelUrl: "https://admin.smith-pharmacy.zenpharm.com.au",
        PlanName: "Basic",
        BillingPeriod: "Monthly");

    [Fact]
    public void Build_SubjectContainsPharmacyName()
    {
        var email = _builder.Build(CreateTestData());
        Assert.Contains("Smith Pharmacy", email.Subject);
        Assert.Contains("Welcome", email.Subject);
    }

    [Fact]
    public void Build_HtmlBodyContainsAdminName()
    {
        var email = _builder.Build(CreateTestData());
        Assert.Contains("John Smith", email.HtmlBody);
    }

    [Fact]
    public void Build_HtmlBodyContainsLoginCredentials()
    {
        var email = _builder.Build(CreateTestData());
        Assert.Contains("john@smithpharmacy.com.au", email.HtmlBody);
        Assert.Contains("TempPass123!", email.HtmlBody);
    }

    [Fact]
    public void Build_HtmlBodyContainsAdminPanelLink()
    {
        var email = _builder.Build(CreateTestData());
        Assert.Contains("https://admin.smith-pharmacy.zenpharm.com.au", email.HtmlBody);
        Assert.Contains("Log In", email.HtmlBody);
    }

    [Fact]
    public void Build_HtmlBodyContainsPlanDetails()
    {
        var email = _builder.Build(CreateTestData());
        Assert.Contains("Basic", email.HtmlBody);
        Assert.Contains("Monthly", email.HtmlBody);
    }

    [Fact]
    public void Build_HtmlBodyContainsPasswordChangeWarning()
    {
        var email = _builder.Build(CreateTestData());
        Assert.Contains("change your password", email.HtmlBody, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Build_HtmlBodyIsValidHtml()
    {
        var email = _builder.Build(CreateTestData());
        Assert.StartsWith("<!DOCTYPE html>", email.HtmlBody.TrimStart());
        Assert.Contains("</html>", email.HtmlBody);
    }
}
