using Dapper;

namespace Api.Common.Migrations;

internal sealed class CatalogMigration(
    ICatalogDb catalogDb,
    ILogger<CatalogMigration> logger) : ICatalogMigration
{
    public async Task RunAllAsync(CancellationToken ct = default)
    {
        const int maxRetries = 3;
        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                using var conn = await catalogDb.CreateAsync();

                foreach (var (name, sql) in CatalogDdl)
                {
                    logger.LogInformation("Running catalogue migration: {Name}", name);
                    await conn.ExecuteAsync(new CommandDefinition(sql, cancellationToken: ct));
                }

                logger.LogInformation("Catalogue migrations completed successfully");
                return;
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                logger.LogWarning(ex, "Catalogue migration attempt {Attempt}/{Max} failed — retrying in {Delay}s",
                    attempt, maxRetries, attempt * 5);
                await Task.Delay(attempt * 5000, ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Catalogue migration skipped — connection unavailable after {Max} attempts", maxRetries);
            }
        }
    }

    private static readonly (string Name, string Sql)[] CatalogDdl =
    [
        ("001_Tenants", """
            IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Tenants' AND schema_id = SCHEMA_ID('dbo'))
            BEGIN
                CREATE TABLE dbo.Tenants (
                    Id              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
                    Subdomain       NVARCHAR(63)     NOT NULL,
                    DisplayName     NVARCHAR(200)    NOT NULL,
                    LogoUrl         NVARCHAR(500)    NULL,
                    PrimaryColour   NVARCHAR(7)      NOT NULL DEFAULT '#1890ff',
                    ContactEmail    NVARCHAR(200)    NULL,
                    ContactPhone    NVARCHAR(20)     NULL,
                    ConnectionString NVARCHAR(500)   NOT NULL, -- TODO: encrypt at rest (Azure Key Vault / Always Encrypted) before production
                    Status          NVARCHAR(20)     NOT NULL DEFAULT 'Active',
                    CreatedAt       DATETIMEOFFSET   NOT NULL DEFAULT SYSUTCDATETIME(),
                    UpdatedAt       DATETIMEOFFSET   NOT NULL DEFAULT SYSUTCDATETIME(),
                    CONSTRAINT PK_Tenants PRIMARY KEY (Id),
                    CONSTRAINT UQ_Tenants_Subdomain UNIQUE (Subdomain),
                    CONSTRAINT CK_Tenants_Status CHECK (Status IN ('Active', 'Suspended', 'Cancelled'))
                );
                CREATE INDEX IX_Tenants_Subdomain ON dbo.Tenants (Subdomain);
                CREATE INDEX IX_Tenants_Status ON dbo.Tenants (Status);
            END
            """),

        ("002_Plans", """
            IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Plans' AND schema_id = SCHEMA_ID('dbo'))
            BEGIN
                CREATE TABLE dbo.Plans (
                    Id              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
                    Name            NVARCHAR(50)     NOT NULL,
                    PriceMonthly    DECIMAL(10,2)    NOT NULL,
                    PriceYearly     DECIMAL(10,2)    NOT NULL,
                    Features        NVARCHAR(MAX)    NULL,
                    MaxUsers        INT              NOT NULL DEFAULT 5,
                    MaxProducts     INT              NOT NULL DEFAULT 500,
                    IsActive        BIT              NOT NULL DEFAULT 1,
                    CreatedAt       DATETIMEOFFSET   NOT NULL DEFAULT SYSUTCDATETIME(),
                    UpdatedAt       DATETIMEOFFSET   NOT NULL DEFAULT SYSUTCDATETIME(),
                    CONSTRAINT PK_Plans PRIMARY KEY (Id),
                    CONSTRAINT UQ_Plans_Name UNIQUE (Name)
                );
            END
            """),

        ("003_Subscriptions", """
            IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Subscriptions' AND schema_id = SCHEMA_ID('dbo'))
            BEGIN
                CREATE TABLE dbo.Subscriptions (
                    Id                   UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
                    TenantId             UNIQUEIDENTIFIER NOT NULL,
                    PlanId               UNIQUEIDENTIFIER NOT NULL,
                    PlanName             NVARCHAR(50)     NOT NULL,
                    Status               NVARCHAR(20)     NOT NULL DEFAULT 'Active',
                    BillingPeriod        NVARCHAR(10)     NOT NULL DEFAULT 'Monthly',
                    StripeCustomerId     NVARCHAR(100)    NULL,
                    StripeSubscriptionId NVARCHAR(100)    NULL,
                    CurrentPeriodStart   DATETIMEOFFSET   NULL,
                    CurrentPeriodEnd     DATETIMEOFFSET   NULL,
                    CreatedAt            DATETIMEOFFSET   NOT NULL DEFAULT SYSUTCDATETIME(),
                    UpdatedAt            DATETIMEOFFSET   NOT NULL DEFAULT SYSUTCDATETIME(),
                    CONSTRAINT PK_Subscriptions PRIMARY KEY (Id),
                    CONSTRAINT FK_Subscriptions_TenantId FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(Id),
                    CONSTRAINT FK_Subscriptions_PlanId FOREIGN KEY (PlanId) REFERENCES dbo.Plans(Id),
                    CONSTRAINT CK_Subscriptions_Status CHECK (Status IN ('Active', 'PastDue', 'Cancelled', 'Trialing')),
                    CONSTRAINT CK_Subscriptions_BillingPeriod CHECK (BillingPeriod IN ('Monthly', 'Yearly'))
                );
                CREATE INDEX IX_Subscriptions_TenantId ON dbo.Subscriptions (TenantId);
                CREATE INDEX IX_Subscriptions_Status ON dbo.Subscriptions (Status);
            END
            """),

        ("004_MasterProducts", """
            IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'MasterProducts' AND schema_id = SCHEMA_ID('dbo'))
            BEGIN
                CREATE TABLE dbo.MasterProducts (
                    Id              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
                    Sku             NVARCHAR(50)     NOT NULL,
                    Name            NVARCHAR(200)    NOT NULL,
                    Category        NVARCHAR(100)    NOT NULL,
                    Description     NVARCHAR(1000)   NULL,
                    UnitPrice       DECIMAL(10,2)    NOT NULL DEFAULT 0,
                    Unit            NVARCHAR(20)     NOT NULL DEFAULT 'each',
                    Metadata        NVARCHAR(MAX)    NULL,
                    IsActive        BIT              NOT NULL DEFAULT 1,
                    CreatedAt       DATETIMEOFFSET   NOT NULL DEFAULT SYSUTCDATETIME(),
                    UpdatedAt       DATETIMEOFFSET   NOT NULL DEFAULT SYSUTCDATETIME(),
                    CONSTRAINT PK_MasterProducts PRIMARY KEY (Id),
                    CONSTRAINT UQ_MasterProducts_Sku UNIQUE (Sku)
                );
                CREATE INDEX IX_MasterProducts_Category ON dbo.MasterProducts (Category);
                CREATE INDEX IX_MasterProducts_IsActive ON dbo.MasterProducts (IsActive) WHERE IsActive = 1;
            END
            """),

        ("005_MasterProducts_PharmacyColumns", """
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.MasterProducts') AND name = 'GenericName')
            BEGIN
                ALTER TABLE dbo.MasterProducts ADD
                    GenericName       NVARCHAR(200)  NULL,
                    Brand             NVARCHAR(200)  NULL,
                    Barcode           NVARCHAR(50)   NULL,
                    ScheduleClass     NVARCHAR(20)   NOT NULL DEFAULT 'Unscheduled',
                    PackSize          NVARCHAR(50)   NULL,
                    ActiveIngredients NVARCHAR(1000) NULL,
                    Warnings          NVARCHAR(2000) NULL,
                    PbsItemCode       NVARCHAR(20)   NULL,
                    ImageUrl          NVARCHAR(500)  NULL;

                EXEC('ALTER TABLE dbo.MasterProducts ADD CONSTRAINT CK_MasterProducts_ScheduleClass CHECK (ScheduleClass IN (''Unscheduled'', ''S2'', ''S3'', ''S4''))');
                EXEC('CREATE INDEX IX_MasterProducts_Barcode ON dbo.MasterProducts (Barcode) WHERE Barcode IS NOT NULL');
                EXEC('CREATE INDEX IX_MasterProducts_ScheduleClass ON dbo.MasterProducts (ScheduleClass)');
                EXEC('CREATE INDEX IX_MasterProducts_GenericName ON dbo.MasterProducts (GenericName) WHERE GenericName IS NOT NULL');
            END
            """),

        ("006_Tenants_PharmacyColumns", """
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Tenants') AND name = 'Abn')
            BEGIN
                ALTER TABLE dbo.Tenants ADD
                    Abn               NVARCHAR(14)   NULL,
                    AddressLine1      NVARCHAR(200)  NULL,
                    AddressLine2      NVARCHAR(200)  NULL,
                    Suburb            NVARCHAR(100)  NULL,
                    State             NVARCHAR(10)   NULL,
                    Postcode          NVARCHAR(10)   NULL,
                    BusinessHoursJson NVARCHAR(MAX)  NULL;
            END
            """),

        ("007_Plans_MaxConcurrentSessions", """
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Plans') AND name = 'MaxConcurrentSessions')
            BEGIN
                ALTER TABLE dbo.Plans ADD MaxConcurrentSessions INT NOT NULL DEFAULT 5;
            END
            """)
    ];
}
