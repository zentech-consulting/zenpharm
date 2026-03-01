using System.Data;
using Microsoft.Data.SqlClient;

namespace Api.Common;

/// <summary>
/// Connects to the Catalog DB (tenant registry, plans, subscriptions).
/// Registered as singleton — one connection string for the platform.
/// Caller must dispose the returned connection (use <c>using</c>).
/// </summary>
public interface ICatalogDb
{
    Task<IDbConnection> CreateAsync();
}

/// <summary>
/// Connects to the current tenant's DB.
/// Registered as scoped — connection string comes from TenantContext.
/// Caller must dispose the returned connection (use <c>using</c>).
/// </summary>
public interface ITenantDb
{
    Task<IDbConnection> CreateAsync();
}

/// <summary>
/// Single-connection-string factory. Used for ICatalogDb (singleton)
/// and as a dev fallback for ITenantDb when no tenant context is available.
/// </summary>
internal sealed class SqlConnectionFactory(string connectionString) : ICatalogDb, ITenantDb
{
    public async Task<IDbConnection> CreateAsync()
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                "Database connection string is not configured.");

        var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();
        return conn;
    }
}

/// <summary>
/// Tenant-scoped connection factory. Created per-request with the
/// connection string from the resolved TenantContext.
/// </summary>
internal sealed class TenantSqlConnectionFactory(string connectionString) : ITenantDb
{
    public async Task<IDbConnection> CreateAsync()
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                "Tenant database connection string is not configured. Ensure tenant resolution succeeded.");

        var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();
        return conn;
    }
}
