-- Catalog DB: Tenants table
-- Stores all registered tenants and their configuration

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
        ConnectionString NVARCHAR(500)   NOT NULL,
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
