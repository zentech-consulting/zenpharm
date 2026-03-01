using System.Collections.Concurrent;
using Api.Common.Security;
using Dapper;

namespace Api.Common.Tenancy;

internal sealed class TenantResolver(
    ICatalogDb catalogDb,
    IConnectionStringProtector protector,
    ILogger<TenantResolver> logger) : ITenantResolver
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new(StringComparer.OrdinalIgnoreCase);

    public async Task<TenantContext?> ResolveAsync(string subdomain, CancellationToken ct = default)
    {
        if (_cache.TryGetValue(subdomain, out var entry) && !entry.IsExpired)
        {
            logger.LogDebug("Tenant cache hit for {Subdomain}", subdomain);
            return entry.Context;
        }

        logger.LogDebug("Tenant cache miss for {Subdomain}, querying catalogue", subdomain);

        TenantEntity? entity;
        try
        {
            entity = await QueryTenantAsync(subdomain, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to query catalogue DB for subdomain {Subdomain}", subdomain);
            return null;
        }

        if (entity is null)
        {
            logger.LogWarning("No tenant found for subdomain {Subdomain}", subdomain);
            return null;
        }

        var context = new TenantContext(
            TenantId: entity.Id,
            Subdomain: entity.Subdomain,
            DisplayName: entity.DisplayName,
            LogoUrl: entity.LogoUrl,
            PrimaryColour: entity.PrimaryColour,
            Plan: entity.PlanName ?? "None",
            Status: entity.Status,
            ConnectionString: protector.Unprotect(entity.ConnectionString));

        _cache[subdomain] = new CacheEntry(context, DateTimeOffset.UtcNow.Add(CacheTtl));

        return context;
    }

    public void InvalidateCache(string subdomain)
    {
        _cache.TryRemove(subdomain, out _);
        logger.LogInformation("Tenant cache invalidated for {Subdomain}", subdomain);
    }

    private async Task<TenantEntity?> QueryTenantAsync(string subdomain, CancellationToken ct)
    {
        const string sql = """
            SELECT
                t.Id,
                t.Subdomain,
                t.DisplayName,
                t.LogoUrl,
                t.PrimaryColour,
                t.ContactEmail,
                t.ContactPhone,
                t.ConnectionString,
                t.Status,
                t.CreatedAt,
                t.UpdatedAt,
                s.PlanName
            FROM dbo.Tenants t
            LEFT JOIN dbo.Subscriptions s ON s.TenantId = t.Id AND s.Status = 'Active'
            WHERE t.Subdomain = @Subdomain
            """;

        using var conn = await catalogDb.CreateAsync();
        return await conn.QuerySingleOrDefaultAsync<TenantEntity>(
            new CommandDefinition(sql, new { Subdomain = subdomain }, cancellationToken: ct));
    }

    private sealed record CacheEntry(TenantContext? Context, DateTimeOffset ExpiresAt)
    {
        public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;
    }
}
