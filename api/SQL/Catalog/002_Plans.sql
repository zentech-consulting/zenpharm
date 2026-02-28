-- Catalog DB: Plans table
-- Defines available subscription plans

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Plans' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.Plans (
        Id              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
        Name            NVARCHAR(50)     NOT NULL,
        PriceMonthly    DECIMAL(10,2)    NOT NULL,
        PriceYearly     DECIMAL(10,2)    NOT NULL,
        Features        NVARCHAR(MAX)    NULL,  -- JSON array of feature flags
        MaxUsers        INT              NOT NULL DEFAULT 5,
        MaxProducts     INT              NOT NULL DEFAULT 500,
        IsActive        BIT              NOT NULL DEFAULT 1,
        CreatedAt       DATETIMEOFFSET   NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt       DATETIMEOFFSET   NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_Plans PRIMARY KEY (Id),
        CONSTRAINT UQ_Plans_Name UNIQUE (Name)
    );
END
