namespace Api.Common.Tenancy;

public interface ITenantResolver
{
    Task<TenantContext?> ResolveAsync(string subdomain, CancellationToken ct = default);
    void InvalidateCache(string subdomain);
}
