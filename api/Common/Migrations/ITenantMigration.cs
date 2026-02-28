namespace Api.Common.Migrations;

public interface ITenantMigration
{
    /// <summary>
    /// Runs all tenant DDL migrations against the given connection string.
    /// Used both at startup (dev tenant) and by the provisioning pipeline (new tenants).
    /// </summary>
    Task RunAllAsync(string connectionString, CancellationToken ct = default);
}
