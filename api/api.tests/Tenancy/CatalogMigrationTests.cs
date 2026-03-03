using Xunit;

namespace Api.Tests.Tenancy;

public class CatalogMigrationTests
{
    [Fact]
    public void Migration008_ContainsIdempotencyCheck()
    {
        // The migration SQL must check IF NOT EXISTS before ALTER TABLE
        // to ensure re-running migrations is safe.
        var migrationSql = GetMigrationSql("008_Tenants_BrandingColumns");
        Assert.Contains("IF NOT EXISTS", migrationSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("SecondaryColour", migrationSql);
    }

    [Fact]
    public void Migration008_AddsAllBrandingColumns()
    {
        var migrationSql = GetMigrationSql("008_Tenants_BrandingColumns");
        Assert.Contains("SecondaryColour", migrationSql);
        Assert.Contains("AccentColour", migrationSql);
        Assert.Contains("HighlightColour", migrationSql);
        Assert.Contains("Tagline", migrationSql);
        Assert.Contains("FaviconUrl", migrationSql);
        Assert.Contains("ShortName", migrationSql);
    }

    [Fact]
    public void Migration009_ContainsIdempotencyCheck()
    {
        var migrationSql = GetMigrationSql("009_PendingSignups");
        Assert.Contains("IF NOT EXISTS", migrationSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("PendingSignups", migrationSql);
    }

    [Fact]
    public void Migration009_CreatesAllRequiredColumns()
    {
        var migrationSql = GetMigrationSql("009_PendingSignups");
        Assert.Contains("PharmacyName", migrationSql);
        Assert.Contains("Subdomain", migrationSql);
        Assert.Contains("AdminEmail", migrationSql);
        Assert.Contains("AdminFullName", migrationSql);
        Assert.Contains("PlanId", migrationSql);
        Assert.Contains("BillingPeriod", migrationSql);
        Assert.Contains("StripeSessionId", migrationSql);
        Assert.Contains("StripeCustomerId", migrationSql);
        Assert.Contains("StripeSubscriptionId", migrationSql);
        Assert.Contains("Status", migrationSql);
        Assert.Contains("TenantId", migrationSql);
        Assert.Contains("FailureReason", migrationSql);
        Assert.Contains("pending_payment", migrationSql);
        Assert.Contains("provisioning", migrationSql);
        Assert.Contains("active", migrationSql);
        Assert.Contains("failed", migrationSql);
        Assert.Contains("expired", migrationSql);
    }

    private static string GetMigrationSql(string name)
    {
        // Access the CatalogDdl field via reflection since it's private static
        var field = typeof(Api.Common.Migrations.CatalogMigration)
            .GetField("CatalogDdl", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Assert.NotNull(field);

        var ddl = ((string Name, string Sql)[])field.GetValue(null)!;
        var entry = ddl.FirstOrDefault(d => d.Name == name);
        Assert.NotNull(entry.Sql);
        return entry.Sql;
    }
}
