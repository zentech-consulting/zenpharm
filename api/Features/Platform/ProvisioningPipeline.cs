using System.Security.Cryptography;
using Api.Common;
using Api.Common.Migrations;
using Api.Common.Security;
using Api.Common.Seeding;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Api.Features.Platform;

internal sealed class ProvisioningPipeline(
    ICatalogDb catalogDb,
    ITenantMigration tenantMigration,
    IConnectionStringProtector protector,
    IConfiguration configuration,
    ILogger<ProvisioningPipeline> logger) : IProvisioningPipeline
{
    public async Task<ProvisionResult> ProvisionAsync(ProvisionRequest request, CancellationToken ct = default)
    {
        logger.LogInformation(
            "Provisioning tenant: {Name} ({Subdomain}), admin: {Email}, plan: {Plan}",
            request.TenantName, request.Subdomain, request.AdminEmail, request.Plan ?? "free");

        try
        {
            // 1. Derive master connection string from catalogue config
            var catalogConnStr = configuration.GetConnectionString("CatalogConnection")
                ?? throw new InvalidOperationException("CatalogConnection is not configured.");
            var masterConnStr = ParseMasterConnectionString(catalogConnStr);

            // 2. Create tenant database
            var dbName = BuildDatabaseName(request.Subdomain);
            await CreateDatabaseAsync(masterConnStr, dbName, ct);
            logger.LogInformation("Database created: {DbName}", dbName);

            // 3. Build tenant connection string
            var tenantConnStr = BuildTenantConnectionString(catalogConnStr, dbName);

            // 4. Run tenant migrations
            await tenantMigration.RunAllAsync(tenantConnStr, ct);
            logger.LogInformation("Tenant migrations completed for {DbName}", dbName);

            // 5. Create admin user
            var password = request.AdminFullName is not null
                ? GeneratePassword()
                : "admin123"; // fallback for legacy ProvisionRequest without AdminFullName
            var adminFullName = request.AdminFullName ?? "Admin";
            await CreateAdminUserAsync(tenantConnStr, request.AdminEmail, adminFullName, password, ct);
            logger.LogInformation("Admin user created for {Email}", request.AdminEmail);

            // 6. Import master products into tenant (filtered by template pack)
            await ImportMasterProductsAsync(tenantConnStr, request.TemplatePack, ct);

            // 7. Register tenant + subscription in catalogue DB
            var encryptedConnStr = protector.Protect(tenantConnStr);
            using var catalogConn = await catalogDb.CreateAsync();

            var tenantId = await catalogConn.QuerySingleAsync<Guid>(
                new CommandDefinition("""
                    DECLARE @Ids TABLE(Id UNIQUEIDENTIFIER);
                    INSERT INTO dbo.Tenants (Subdomain, DisplayName, ConnectionString, ContactEmail, Status)
                    OUTPUT INSERTED.Id INTO @Ids
                    VALUES (@Subdomain, @DisplayName, @ConnectionString, @ContactEmail, 'Active');
                    SELECT Id FROM @Ids;
                    """,
                    new
                    {
                        request.Subdomain,
                        DisplayName = request.TenantName,
                        ConnectionString = encryptedConnStr,
                        ContactEmail = request.AdminEmail
                    }, cancellationToken: ct));

            // Create subscription if plan info provided
            if (request.PlanId.HasValue)
            {
                await catalogConn.ExecuteAsync(
                    new CommandDefinition("""
                        INSERT INTO dbo.Subscriptions (TenantId, PlanId, PlanName, Status, BillingPeriod,
                            StripeCustomerId, StripeSubscriptionId, CurrentPeriodStart)
                        VALUES (@TenantId, @PlanId, @PlanName, 'Active', @BillingPeriod,
                            @StripeCustomerId, @StripeSubscriptionId, SYSUTCDATETIME());
                        """,
                        new
                        {
                            TenantId = tenantId,
                            request.PlanId,
                            PlanName = request.Plan ?? "Basic",
                            BillingPeriod = request.BillingPeriod ?? "Monthly",
                            request.StripeCustomerId,
                            request.StripeSubscriptionId
                        }, cancellationToken: ct));
            }

            logger.LogInformation("Tenant provisioned: {TenantId} ({Subdomain})", tenantId, request.Subdomain);

            return new ProvisionResult(
                Success: true,
                TenantId: tenantId.ToString(),
                Message: "Tenant provisioned successfully.",
                AdminPassword: password);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Provisioning failed for {Subdomain}", request.Subdomain);
            return new ProvisionResult(
                Success: false,
                TenantId: null,
                Message: $"Provisioning failed: {ex.Message}");
        }
    }

    // ── Testable Static Helpers ────────────────────────────────────

    /// <summary>
    /// Replaces the Database segment in a connection string with 'master'.
    /// </summary>
    internal static string ParseMasterConnectionString(string catalogConnStr)
    {
        var builder = new SqlConnectionStringBuilder(catalogConnStr)
        {
            InitialCatalog = "master"
        };
        return builder.ConnectionString;
    }

    /// <summary>
    /// Builds a sanitised database name from a subdomain.
    /// </summary>
    internal static string BuildDatabaseName(string subdomain)
    {
        // Sanitise: only alphanumeric + underscore (hyphens become underscores)
        var sanitised = subdomain.Replace('-', '_');
        return $"ZenPharmTenant_{sanitised}";
    }

    /// <summary>
    /// Replaces the Database segment in a connection string with the tenant DB name.
    /// </summary>
    internal static string BuildTenantConnectionString(string catalogConnStr, string dbName)
    {
        var builder = new SqlConnectionStringBuilder(catalogConnStr)
        {
            InitialCatalog = dbName
        };
        return builder.ConnectionString;
    }

    /// <summary>
    /// Generates a 16-character random password using cryptographic RNG.
    /// </summary>
    internal static string GeneratePassword()
    {
        const string chars = "abcdefghijkmnpqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var bytes = RandomNumberGenerator.GetBytes(16);
        var password = new char[16];
        for (var i = 0; i < 16; i++)
            password[i] = chars[bytes[i] % chars.Length];
        return new string(password);
    }

    // ── Private Implementation ─────────────────────────────────────

    private static async Task CreateDatabaseAsync(string masterConnStr, string dbName, CancellationToken ct)
    {
        await using var conn = new SqlConnection(masterConnStr);
        await conn.OpenAsync(ct);

        // Use parameterised check but dynamic SQL for CREATE DATABASE (DDL doesn't support parameters)
        var exists = await conn.ExecuteScalarAsync<int>(
            new CommandDefinition(
                "SELECT COUNT(*) FROM sys.databases WHERE name = @Name",
                new { Name = dbName }, cancellationToken: ct));

        if (exists > 0)
        {
            // Database already exists — skip creation
            return;
        }

        // CREATE DATABASE requires raw SQL (no parameter binding for DB name)
        // The dbName is derived from subdomain which is regex-validated: [a-z0-9_]
        await conn.ExecuteAsync(
            new CommandDefinition($"CREATE DATABASE [{dbName}]", cancellationToken: ct));
    }

    private static async Task CreateAdminUserAsync(
        string tenantConnStr, string email, string fullName, string password, CancellationToken ct)
    {
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

        await using var conn = new SqlConnection(tenantConnStr);
        await conn.OpenAsync(ct);

        await conn.ExecuteAsync(
            new CommandDefinition("""
                IF NOT EXISTS (SELECT 1 FROM dbo.AdminUsers WHERE Email = @Email)
                BEGIN
                    INSERT INTO dbo.AdminUsers (Username, Email, PasswordHash, FullName, Role)
                    VALUES (@Username, @Email, @PasswordHash, @FullName, 'SuperAdmin');
                END
                """,
                new
                {
                    Username = email,
                    Email = email,
                    PasswordHash = passwordHash,
                    FullName = fullName
                }, cancellationToken: ct));
    }

    private async Task ImportMasterProductsAsync(
        string tenantConnStr, string? templatePackId, CancellationToken ct)
    {
        var pack = !string.IsNullOrWhiteSpace(templatePackId)
            ? TemplatePacks.GetById(templatePackId) ?? TemplatePacks.GetDefault()
            : TemplatePacks.GetDefault();

        using var catalogConn = await catalogDb.CreateAsync();
        var masterProducts = await catalogConn.QueryAsync<(
            Guid Id, string Name, string? GenericName, string? Brand, string Category,
            string ScheduleClass, decimal UnitPrice)>(
            new CommandDefinition(
                "SELECT Id, Name, GenericName, Brand, Category, ScheduleClass, UnitPrice FROM dbo.MasterProducts WHERE IsActive = 1",
                cancellationToken: ct));

        var filtered = masterProducts
            .Where(p => TemplatePacks.IncludesCategory(pack, p.Category))
            .ToArray();

        if (filtered.Length == 0) return;

        await using var tenantConn = new SqlConnection(tenantConnStr);
        await tenantConn.OpenAsync(ct);

        var sortOrder = 0;
        var rng = new Random(42);
        foreach (var mp in filtered)
        {
            sortOrder++;
            var stockQty = rng.Next(10, 90);
            var reorderLevel = mp.ScheduleClass switch
            {
                "S4" => 5,
                "S3" => 10,
                _ => 15
            };

            await tenantConn.ExecuteAsync(
                new CommandDefinition("""
                    IF NOT EXISTS (SELECT 1 FROM dbo.TenantProducts WHERE MasterProductId = @MasterProductId)
                    BEGIN
                        INSERT INTO dbo.TenantProducts
                            (MasterProductId, MasterProductName, GenericName, Brand, Category,
                             ScheduleClass, DefaultPrice, StockQuantity, ReorderLevel, ExpiryDate,
                             IsVisible, IsFeatured, SortOrder)
                        VALUES
                            (@MasterProductId, @MasterProductName, @GenericName, @Brand, @Category,
                             @ScheduleClass, @DefaultPrice, @StockQuantity, @ReorderLevel, @ExpiryDate,
                             1, @IsFeatured, @SortOrder);
                    END
                    """,
                    new
                    {
                        MasterProductId = mp.Id,
                        MasterProductName = mp.Name,
                        GenericName = mp.GenericName,
                        Brand = mp.Brand,
                        Category = mp.Category,
                        ScheduleClass = mp.ScheduleClass,
                        DefaultPrice = mp.UnitPrice,
                        StockQuantity = stockQty,
                        ReorderLevel = reorderLevel,
                        ExpiryDate = DateTime.UtcNow.AddMonths(rng.Next(3, 28)),
                        IsFeatured = sortOrder <= 20,
                        SortOrder = sortOrder
                    }, cancellationToken: ct));
        }

        logger.LogInformation("Imported {Count} products from template pack '{Pack}'", filtered.Length, pack.Name);
    }
}
