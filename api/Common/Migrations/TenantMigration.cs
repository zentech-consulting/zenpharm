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
            """)
    ];
}
