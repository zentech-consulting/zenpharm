namespace Api.Common.Tenancy;

/// <summary>
/// Immutable tenant context injected per-request by TenantMiddleware.
/// Contains everything downstream code needs to operate within a tenant scope.
/// </summary>
public sealed record TenantContext(
    Guid TenantId,
    string Subdomain,
    string DisplayName,
    string? LogoUrl,
    string PrimaryColour,
    string Plan,
    string Status,
    string ConnectionString)
{
    public bool IsActive => Status.Equals("Active", StringComparison.OrdinalIgnoreCase);
}
