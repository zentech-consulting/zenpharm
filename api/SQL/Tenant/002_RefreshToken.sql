-- Tenant DB: RefreshTokens table
-- JWT refresh tokens with SHA256 hashing and revocation support.

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'RefreshTokens' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.RefreshTokens (
        Id              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
        UserId          UNIQUEIDENTIFIER NOT NULL,
        TokenHash       NVARCHAR(128)    NOT NULL,  -- SHA256 of the actual token
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
