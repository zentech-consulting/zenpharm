namespace Api.Common.Tenancy;

/// <summary>
/// Internal DB entity mapped from dbo.Tenants JOIN dbo.Subscriptions in the Catalog DB.
/// </summary>
internal sealed class TenantEntity
{
    public Guid Id { get; init; }
    public string Subdomain { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public string? LogoUrl { get; init; }
    public string PrimaryColour { get; init; } = "#1890ff";
    public string? ContactEmail { get; init; }
    public string? ContactPhone { get; init; }
    public string ConnectionString { get; init; } = "";
    public string Status { get; init; } = "Active";
    public string? PlanName { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}
