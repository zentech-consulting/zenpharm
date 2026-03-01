-- Catalog DB: MasterProducts table
-- Generic product catalogue shared across tenants.
-- Industry-specific fields stored in Metadata JSON column.

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
        Metadata        NVARCHAR(MAX)    NULL,  -- Industry-specific JSON
        IsActive        BIT              NOT NULL DEFAULT 1,
        CreatedAt       DATETIMEOFFSET   NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt       DATETIMEOFFSET   NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_MasterProducts PRIMARY KEY (Id),
        CONSTRAINT UQ_MasterProducts_Sku UNIQUE (Sku)
    );

    CREATE INDEX IX_MasterProducts_Category ON dbo.MasterProducts (Category);
    CREATE INDEX IX_MasterProducts_IsActive ON dbo.MasterProducts (IsActive) WHERE IsActive = 1;
END
