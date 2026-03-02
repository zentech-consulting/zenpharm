using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Api.Common.Migrations;

internal sealed class TenantMigration(
    ILogger<TenantMigration> logger) : ITenantMigration
{
    public async Task RunAllAsync(string connectionString, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            logger.LogWarning("Tenant migration skipped — no connection string provided");
            return;
        }

        try
        {
            await using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync(ct);

            foreach (var (name, sql) in TenantDdl)
            {
                logger.LogInformation("Running tenant migration: {Name}", name);
                await conn.ExecuteAsync(new CommandDefinition(sql, cancellationToken: ct));
            }

            logger.LogInformation("Tenant migrations completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Tenant migration failed");
        }
    }

    private static readonly (string Name, string Sql)[] TenantDdl =
    [
        ("001_AdminUser", """
            IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AdminUsers' AND schema_id = SCHEMA_ID('dbo'))
            BEGIN
                CREATE TABLE dbo.AdminUsers (
                    Id                  UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
                    Username            NVARCHAR(100)    NOT NULL,
                    Email               NVARCHAR(200)    NOT NULL,
                    PasswordHash        NVARCHAR(200)    NOT NULL,
                    FullName            NVARCHAR(200)    NOT NULL,
                    Role                NVARCHAR(50)     NOT NULL DEFAULT 'Admin',
                    IsActive            BIT              NOT NULL DEFAULT 1,
                    FailedLoginAttempts INT              NOT NULL DEFAULT 0,
                    LockoutEnd          DATETIMEOFFSET   NULL,
                    LastLoginAt         DATETIMEOFFSET   NULL,
                    LastLoginIp         NVARCHAR(45)     NULL,
                    CreatedAt           DATETIMEOFFSET   NOT NULL DEFAULT SYSUTCDATETIME(),
                    UpdatedAt           DATETIMEOFFSET   NOT NULL DEFAULT SYSUTCDATETIME(),
                    CONSTRAINT PK_AdminUsers PRIMARY KEY (Id),
                    CONSTRAINT UQ_AdminUsers_Username UNIQUE (Username),
                    CONSTRAINT UQ_AdminUsers_Email UNIQUE (Email),
                    CONSTRAINT CK_AdminUsers_Role CHECK (Role IN ('SuperAdmin', 'Admin', 'Manager', 'Staff'))
                );
            END
            """),

        ("002_RefreshToken", """
            IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'RefreshTokens' AND schema_id = SCHEMA_ID('dbo'))
            BEGIN
                CREATE TABLE dbo.RefreshTokens (
                    Id              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
                    UserId          UNIQUEIDENTIFIER NOT NULL,
                    TokenHash       NVARCHAR(128)    NOT NULL,
                    ExpiresAt       DATETIMEOFFSET   NOT NULL,
                    IsRevoked       BIT              NOT NULL DEFAULT 0,
                    RevokedAt       DATETIMEOFFSET   NULL,
                    CreatedAt       DATETIMEOFFSET   NOT NULL DEFAULT SYSUTCDATETIME(),
                    CreatedByIp     NVARCHAR(45)     NULL,
                    CONSTRAINT PK_RefreshTokens PRIMARY KEY (Id),
                    CONSTRAINT FK_RefreshTokens_UserId FOREIGN KEY (UserId) REFERENCES dbo.AdminUsers(Id) ON DELETE CASCADE
                );
                CREATE INDEX IX_RefreshTokens_TokenHash ON dbo.RefreshTokens (TokenHash);
                CREATE INDEX IX_RefreshTokens_UserId ON dbo.RefreshTokens (UserId);
                CREATE INDEX IX_RefreshTokens_ExpiresAt ON dbo.RefreshTokens (ExpiresAt) WHERE IsRevoked = 0;
            END
            """),

        ("003_Clients", """
            IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Clients' AND schema_id = SCHEMA_ID('dbo'))
            BEGIN
                CREATE TABLE dbo.Clients (
                    Id          UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
                    FirstName   NVARCHAR(100)    NOT NULL,
                    LastName    NVARCHAR(100)    NOT NULL,
                    Email       NVARCHAR(200)    NULL,
                    Phone       NVARCHAR(20)     NULL,
                    Notes       NVARCHAR(2000)   NULL,
                    CreatedAt   DATETIMEOFFSET   NOT NULL DEFAULT SYSUTCDATETIME(),
                    UpdatedAt   DATETIMEOFFSET   NOT NULL DEFAULT SYSUTCDATETIME(),
                    CONSTRAINT PK_Clients PRIMARY KEY (Id)
                );
                CREATE INDEX IX_Clients_Email ON dbo.Clients (Email);
                CREATE INDEX IX_Clients_LastName ON dbo.Clients (LastName);
            END
            """),

        ("004_Services", """
            IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Services' AND schema_id = SCHEMA_ID('dbo'))
            BEGIN
                CREATE TABLE dbo.Services (
                    Id              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
                    Name            NVARCHAR(200)    NOT NULL,
                    Description     NVARCHAR(MAX)    NULL,
                    Category        NVARCHAR(50)     NOT NULL,
                    Price           DECIMAL(18,2)    NOT NULL,
                    DurationMinutes INT              NOT NULL DEFAULT 30,
                    IsActive        BIT              NOT NULL DEFAULT 1,
                    CreatedAt       DATETIMEOFFSET   NOT NULL DEFAULT SYSUTCDATETIME(),
                    UpdatedAt       DATETIMEOFFSET   NOT NULL DEFAULT SYSUTCDATETIME(),
                    CONSTRAINT PK_Services PRIMARY KEY (Id)
                );
                CREATE INDEX IX_Services_Category ON dbo.Services (Category);
                CREATE INDEX IX_Services_IsActive ON dbo.Services (IsActive);
            END
            """),

        ("005_Employees", """
            IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Employees' AND schema_id = SCHEMA_ID('dbo'))
            BEGIN
                CREATE TABLE dbo.Employees (
                    Id          UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
                    FirstName   NVARCHAR(100)    NOT NULL,
                    LastName    NVARCHAR(100)    NOT NULL,
                    Email       NVARCHAR(200)    NULL,
                    Phone       NVARCHAR(20)     NULL,
                    Role        NVARCHAR(50)     NOT NULL DEFAULT 'staff',
                    IsActive    BIT              NOT NULL DEFAULT 1,
                    CreatedAt   DATETIMEOFFSET   NOT NULL DEFAULT SYSUTCDATETIME(),
                    UpdatedAt   DATETIMEOFFSET   NOT NULL DEFAULT SYSUTCDATETIME(),
                    CONSTRAINT PK_Employees PRIMARY KEY (Id)
                );
                CREATE INDEX IX_Employees_Role ON dbo.Employees (Role);
                CREATE INDEX IX_Employees_IsActive ON dbo.Employees (IsActive);
            END
            """),

        ("006_Bookings", """
            IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Bookings' AND schema_id = SCHEMA_ID('dbo'))
            BEGIN
                CREATE TABLE dbo.Bookings (
                    Id          UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
                    ClientId    UNIQUEIDENTIFIER NOT NULL,
                    ServiceId   UNIQUEIDENTIFIER NOT NULL,
                    EmployeeId  UNIQUEIDENTIFIER NULL,
                    StartTime   DATETIMEOFFSET   NOT NULL,
                    EndTime     DATETIMEOFFSET   NOT NULL,
                    Status      NVARCHAR(30)     NOT NULL DEFAULT 'pending',
                    Notes       NVARCHAR(2000)   NULL,
                    CreatedAt   DATETIMEOFFSET   NOT NULL DEFAULT SYSUTCDATETIME(),
                    UpdatedAt   DATETIMEOFFSET   NOT NULL DEFAULT SYSUTCDATETIME(),
                    CONSTRAINT PK_Bookings PRIMARY KEY (Id),
                    CONSTRAINT FK_Bookings_ClientId FOREIGN KEY (ClientId) REFERENCES dbo.Clients(Id),
                    CONSTRAINT FK_Bookings_ServiceId FOREIGN KEY (ServiceId) REFERENCES dbo.Services(Id),
                    CONSTRAINT FK_Bookings_EmployeeId FOREIGN KEY (EmployeeId) REFERENCES dbo.Employees(Id),
                    CONSTRAINT CK_Bookings_Status CHECK (Status IN ('pending','confirmed','cancelled','completed','no_show'))
                );
                CREATE INDEX IX_Bookings_ClientId ON dbo.Bookings (ClientId);
                CREATE INDEX IX_Bookings_EmployeeId ON dbo.Bookings (EmployeeId);
                CREATE INDEX IX_Bookings_StartTime ON dbo.Bookings (StartTime);
                CREATE INDEX IX_Bookings_Status ON dbo.Bookings (Status);
            END
            """),

        ("007_Schedules", """
            IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Schedules' AND schema_id = SCHEMA_ID('dbo'))
            BEGIN
                CREATE TABLE dbo.Schedules (
                    Id          UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
                    EmployeeId  UNIQUEIDENTIFIER NOT NULL,
                    Date        DATE             NOT NULL,
                    StartTime   TIME             NOT NULL,
                    EndTime     TIME             NOT NULL,
                    Location    NVARCHAR(50)     NULL,
                    Notes       NVARCHAR(2000)   NULL,
                    CreatedAt   DATETIMEOFFSET   NOT NULL DEFAULT SYSUTCDATETIME(),
                    UpdatedAt   DATETIMEOFFSET   NOT NULL DEFAULT SYSUTCDATETIME(),
                    CONSTRAINT PK_Schedules PRIMARY KEY (Id),
                    CONSTRAINT FK_Schedules_EmployeeId FOREIGN KEY (EmployeeId) REFERENCES dbo.Employees(Id)
                );
                CREATE INDEX IX_Schedules_EmployeeId_Date ON dbo.Schedules (EmployeeId, Date);
            END
            """),

        ("008_KnowledgeEntries", """
            IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'KnowledgeEntries' AND schema_id = SCHEMA_ID('dbo'))
            BEGIN
                CREATE TABLE dbo.KnowledgeEntries (
                    Id          UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
                    Title       NVARCHAR(200)    NOT NULL,
                    Content     NVARCHAR(MAX)    NOT NULL,
                    Category    NVARCHAR(50)     NOT NULL,
                    Tags        NVARCHAR(500)    NULL,
                    CreatedAt   DATETIMEOFFSET   NOT NULL DEFAULT SYSUTCDATETIME(),
                    UpdatedAt   DATETIMEOFFSET   NOT NULL DEFAULT SYSUTCDATETIME(),
                    CONSTRAINT PK_KnowledgeEntries PRIMARY KEY (Id)
                );
                CREATE INDEX IX_KnowledgeEntries_Category ON dbo.KnowledgeEntries (Category);
            END
            """),

        ("009_AiChatSessions", """
            IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AiChatSessions' AND schema_id = SCHEMA_ID('dbo'))
            BEGIN
                CREATE TABLE dbo.AiChatSessions (
                    Id              BIGINT IDENTITY(1,1) NOT NULL,
                    SessionToken    NVARCHAR(100)   NOT NULL,
                    ClientIp        NVARCHAR(45)    NULL,
                    MessageCount    INT             NOT NULL DEFAULT 0,
                    CreatedAt       DATETIMEOFFSET  NOT NULL DEFAULT SYSUTCDATETIME(),
                    LastMessageAt   DATETIMEOFFSET  NOT NULL DEFAULT SYSUTCDATETIME(),
                    CONSTRAINT PK_AiChatSessions PRIMARY KEY (Id)
                );
                CREATE UNIQUE INDEX IX_AiChatSessions_SessionToken ON dbo.AiChatSessions (SessionToken);
            END
            """),

        ("010_AiChatMessages", """
            IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AiChatMessages' AND schema_id = SCHEMA_ID('dbo'))
            BEGIN
                CREATE TABLE dbo.AiChatMessages (
                    Id          BIGINT IDENTITY(1,1) NOT NULL,
                    SessionId   BIGINT           NOT NULL,
                    Role        NVARCHAR(20)     NOT NULL,
                    Content     NVARCHAR(MAX)    NOT NULL,
                    ToolsCalled NVARCHAR(500)    NULL,
                    DurationMs  INT              NULL,
                    Success     BIT              NOT NULL DEFAULT 1,
                    Error       NVARCHAR(MAX)    NULL,
                    CreatedAt   DATETIMEOFFSET   NOT NULL DEFAULT SYSUTCDATETIME(),
                    CONSTRAINT PK_AiChatMessages PRIMARY KEY (Id),
                    CONSTRAINT FK_AiChatMessages_SessionId FOREIGN KEY (SessionId) REFERENCES dbo.AiChatSessions(Id) ON DELETE CASCADE
                );
                CREATE INDEX IX_AiChatMessages_SessionId ON dbo.AiChatMessages (SessionId);
            END
            """),

        ("011_Clients_PharmacyColumns", """
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Clients') AND name = 'DateOfBirth')
            BEGIN
                ALTER TABLE dbo.Clients ADD
                    DateOfBirth     DATE           NULL,
                    Allergies       NVARCHAR(2000) NULL,
                    MedicationNotes NVARCHAR(2000) NULL,
                    Tags            NVARCHAR(500)  NULL;
            END
            """),

        ("012_Employees_PharmacyRoles", """
            IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Employees_Role')
            BEGIN
                ALTER TABLE dbo.Employees DROP CONSTRAINT CK_Employees_Role;
            END

            IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Employees_PharmacyRole')
            BEGIN
                ALTER TABLE dbo.Employees ADD CONSTRAINT CK_Employees_PharmacyRole
                    CHECK (Role IN ('pharmacist', 'dispense_technician', 'pharmacy_assistant', 'cashier', 'manager', 'staff'));
            END
            """),

        ("013_TenantProducts", """
            IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TenantProducts' AND schema_id = SCHEMA_ID('dbo'))
            BEGIN
                CREATE TABLE dbo.TenantProducts (
                    Id                UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
                    MasterProductId   UNIQUEIDENTIFIER NOT NULL,
                    MasterProductName NVARCHAR(200)    NOT NULL,
                    GenericName       NVARCHAR(200)    NULL,
                    Brand             NVARCHAR(200)    NULL,
                    Category          NVARCHAR(100)    NOT NULL,
                    ScheduleClass     NVARCHAR(20)     NOT NULL DEFAULT 'Unscheduled',
                    DefaultPrice      DECIMAL(10,2)    NOT NULL DEFAULT 0,
                    CustomName        NVARCHAR(200)    NULL,
                    CustomPrice       DECIMAL(10,2)    NULL,
                    ImageUrl          NVARCHAR(500)    NULL,
                    StockQuantity     INT              NOT NULL DEFAULT 0,
                    ReorderLevel      INT              NOT NULL DEFAULT 10,
                    ExpiryDate        DATE             NULL,
                    IsVisible         BIT              NOT NULL DEFAULT 1,
                    IsFeatured        BIT              NOT NULL DEFAULT 0,
                    SortOrder         INT              NOT NULL DEFAULT 0,
                    CreatedAt         DATETIMEOFFSET   NOT NULL DEFAULT SYSUTCDATETIME(),
                    UpdatedAt         DATETIMEOFFSET   NOT NULL DEFAULT SYSUTCDATETIME(),
                    CONSTRAINT PK_TenantProducts PRIMARY KEY (Id),
                    CONSTRAINT UQ_TenantProducts_MasterProductId UNIQUE (MasterProductId),
                    CONSTRAINT CK_TenantProducts_ScheduleClass CHECK (ScheduleClass IN ('Unscheduled', 'S2', 'S3', 'S4'))
                );
                CREATE INDEX IX_TenantProducts_Category ON dbo.TenantProducts (Category);
                CREATE INDEX IX_TenantProducts_ScheduleClass ON dbo.TenantProducts (ScheduleClass);
                CREATE INDEX IX_TenantProducts_StockQuantity ON dbo.TenantProducts (StockQuantity);
                CREATE INDEX IX_TenantProducts_ExpiryDate ON dbo.TenantProducts (ExpiryDate) WHERE ExpiryDate IS NOT NULL;
            END
            """),

        ("014_StockMovements", """
            IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'StockMovements' AND schema_id = SCHEMA_ID('dbo'))
            BEGIN
                CREATE TABLE dbo.StockMovements (
                    Id              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
                    TenantProductId UNIQUEIDENTIFIER NOT NULL,
                    MovementType    NVARCHAR(20)     NOT NULL,
                    Quantity        INT              NOT NULL,
                    Reference       NVARCHAR(200)    NULL,
                    Notes           NVARCHAR(2000)   NULL,
                    CreatedAt       DATETIMEOFFSET   NOT NULL DEFAULT SYSUTCDATETIME(),
                    CreatedBy       NVARCHAR(200)    NULL,
                    CONSTRAINT PK_StockMovements PRIMARY KEY (Id),
                    CONSTRAINT FK_StockMovements_TenantProductId FOREIGN KEY (TenantProductId) REFERENCES dbo.TenantProducts(Id),
                    CONSTRAINT CK_StockMovements_MovementType CHECK (MovementType IN ('stock_in', 'stock_out', 'adjustment', 'expired', 'return'))
                );
                CREATE INDEX IX_StockMovements_TenantProductId ON dbo.StockMovements (TenantProductId);
                CREATE INDEX IX_StockMovements_CreatedAt ON dbo.StockMovements (CreatedAt);
            END
            """),

        ("015_RefreshTokens_LastUsedAt", """
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.RefreshTokens') AND name = 'LastUsedAt')
            BEGIN
                ALTER TABLE dbo.RefreshTokens ADD LastUsedAt DATETIMEOFFSET NULL;
            END
            """),

        ("016_StockMovements_ApprovedBy", """
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.StockMovements') AND name = 'ApprovedBy')
            BEGIN
                ALTER TABLE dbo.StockMovements ADD ApprovedBy NVARCHAR(200) NULL;
            END
            """)
    ];
}
