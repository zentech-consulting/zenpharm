namespace Api.Features.Platform;

internal interface IWelcomeEmailBuilder
{
    WelcomeEmail Build(WelcomeEmailData data);
}

internal sealed record WelcomeEmailData(
    string PharmacyName,
    string AdminFullName,
    string AdminEmail,
    string TemporaryPassword,
    string AdminPanelUrl,
    string PlanName,
    string BillingPeriod);

internal sealed record WelcomeEmail(string Subject, string HtmlBody);
