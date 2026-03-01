using Api.Common.Migrations;
using Dapper;

namespace Api.Common.Seeding;

internal sealed class DevSeedService(
    ICatalogDb catalogDb,
    ITenantMigration tenantMigration,
    IConfiguration configuration,
    ILogger<DevSeedService> logger) : IDevSeedService
{
    public async Task SeedAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Dev seed starting...");

        var tenantConnString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(tenantConnString))
        {
            logger.LogWarning("Dev seed skipped — DefaultConnection not configured");
            return;
        }

        var subdomain = configuration["Tenancy:DevTenantSubdomain"] ?? "dev";

        try
        {
            using var conn = await catalogDb.CreateAsync();

            // 1. Seed Basic plan
            var planId = await SeedPlanAsync(conn, ct);
            if (planId is null)
            {
                logger.LogWarning("Dev seed: could not seed plan — skipping remaining steps");
                return;
            }

            // 2. Seed dev tenant
            var tenantId = await SeedTenantAsync(conn, subdomain, tenantConnString, ct);
            if (tenantId is null)
            {
                logger.LogWarning("Dev seed: could not seed tenant — skipping remaining steps");
                return;
            }

            // 3. Seed subscription
            await SeedSubscriptionAsync(conn, tenantId.Value, planId.Value, ct);

            // 4. Run tenant migrations on the dev tenant DB
            await tenantMigration.RunAllAsync(tenantConnString, ct);

            // 5. Seed admin user in tenant DB
            await SeedAdminUserAsync(tenantConnString, ct);

            // 6. Seed master products
            await SeedMasterProductsAsync(conn, ct);

            logger.LogInformation("Dev seed complete. Admin user: admin (see seed data for password)");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Dev seed failed — the application will continue without seed data");
        }
    }

    private static async Task<Guid?> SeedPlanAsync(
        System.Data.IDbConnection conn, CancellationToken ct)
    {
        const string sql = """
            IF NOT EXISTS (SELECT 1 FROM dbo.Plans WHERE Name = 'Basic')
            BEGIN
                DECLARE @PlanId TABLE(Id UNIQUEIDENTIFIER);
                INSERT INTO dbo.Plans (Name, PriceMonthly, PriceYearly, Features, MaxUsers, MaxProducts)
                OUTPUT INSERTED.Id INTO @PlanId
                VALUES ('Basic', 79.00, 790.00, '{"products":500,"users":5,"support":"email"}', 5, 500);
                SELECT Id FROM @PlanId;
            END
            ELSE
            BEGIN
                SELECT Id FROM dbo.Plans WHERE Name = 'Basic';
            END
            """;

        return await conn.QuerySingleOrDefaultAsync<Guid?>(
            new CommandDefinition(sql, cancellationToken: ct));
    }

    private static async Task<Guid?> SeedTenantAsync(
        System.Data.IDbConnection conn, string subdomain, string connectionString, CancellationToken ct)
    {
        const string sql = """
            IF NOT EXISTS (SELECT 1 FROM dbo.Tenants WHERE Subdomain = @Subdomain)
            BEGIN
                DECLARE @TenantId TABLE(Id UNIQUEIDENTIFIER);
                INSERT INTO dbo.Tenants (Subdomain, DisplayName, ConnectionString, ContactEmail)
                OUTPUT INSERTED.Id INTO @TenantId
                VALUES (@Subdomain, 'Dev Pharmacy', @ConnectionString, 'dev@zenpharm.local');
                SELECT Id FROM @TenantId;
            END
            ELSE
            BEGIN
                SELECT Id FROM dbo.Tenants WHERE Subdomain = @Subdomain;
            END
            """;

        return await conn.QuerySingleOrDefaultAsync<Guid?>(
            new CommandDefinition(sql, new { Subdomain = subdomain, ConnectionString = connectionString }, cancellationToken: ct));
    }

    private static async Task SeedSubscriptionAsync(
        System.Data.IDbConnection conn, Guid tenantId, Guid planId, CancellationToken ct)
    {
        const string sql = """
            IF NOT EXISTS (SELECT 1 FROM dbo.Subscriptions WHERE TenantId = @TenantId)
            BEGIN
                INSERT INTO dbo.Subscriptions (TenantId, PlanId, PlanName, Status, BillingPeriod)
                VALUES (@TenantId, @PlanId, 'Basic', 'Active', 'Monthly');
            END
            """;

        await conn.ExecuteAsync(
            new CommandDefinition(sql, new { TenantId = tenantId, PlanId = planId }, cancellationToken: ct));
    }

    private static async Task SeedAdminUserAsync(string connectionString, CancellationToken ct)
    {
        await using var conn = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
        await conn.OpenAsync(ct);

        var passwordHash = BCrypt.Net.BCrypt.HashPassword("admin123");

        const string sql = """
            IF NOT EXISTS (SELECT 1 FROM dbo.AdminUsers WHERE Username = 'admin')
            BEGIN
                INSERT INTO dbo.AdminUsers (Username, Email, PasswordHash, FullName, Role)
                VALUES ('admin', 'admin@zenpharm.local', @PasswordHash, 'Dev Admin', 'SuperAdmin');
            END
            """;

        await conn.ExecuteAsync(
            new CommandDefinition(sql, new { PasswordHash = passwordHash }, cancellationToken: ct));
    }

    private static async Task SeedMasterProductsAsync(
        System.Data.IDbConnection conn, CancellationToken ct)
    {
        const string sql = """
            IF NOT EXISTS (SELECT 1 FROM dbo.MasterProducts WHERE Sku = @Sku)
            BEGIN
                INSERT INTO dbo.MasterProducts
                    (Sku, Name, Category, Description, UnitPrice, GenericName, Brand, Barcode,
                     ScheduleClass, PackSize, ActiveIngredients, Warnings)
                VALUES
                    (@Sku, @Name, @Category, @Description, @UnitPrice, @GenericName, @Brand, @Barcode,
                     @ScheduleClass, @PackSize, @ActiveIngredients, @Warnings);
            END
            """;

        foreach (var product in PharmacyMasterProductData.All)
        {
            await conn.ExecuteAsync(new CommandDefinition(sql, new
            {
                product.Sku,
                product.Name,
                product.Category,
                product.Description,
                product.UnitPrice,
                product.GenericName,
                product.Brand,
                product.Barcode,
                product.ScheduleClass,
                product.PackSize,
                product.ActiveIngredients,
                product.Warnings
            }, cancellationToken: ct));
        }
    }
}
