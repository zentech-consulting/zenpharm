-- Catalog DB: Subscriptions table
-- Links tenants to their active plan with Stripe billing info

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Subscriptions' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.Subscriptions (
        Id                  UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
        TenantId            UNIQUEIDENTIFIER NOT NULL,
        PlanId              UNIQUEIDENTIFIER NOT NULL,
        PlanName            NVARCHAR(50)     NOT NULL,  -- Denormalised for fast reads
        Status              NVARCHAR(20)     NOT NULL DEFAULT 'Active',
        BillingPeriod       NVARCHAR(10)     NOT NULL DEFAULT 'Monthly',
        StripeCustomerId    NVARCHAR(100)    NULL,
        StripeSubscriptionId NVARCHAR(100)   NULL,
        CurrentPeriodStart  DATETIMEOFFSET   NULL,
        CurrentPeriodEnd    DATETIMEOFFSET   NULL,
        CreatedAt           DATETIMEOFFSET   NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt           DATETIMEOFFSET   NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_Subscriptions PRIMARY KEY (Id),
        CONSTRAINT FK_Subscriptions_TenantId FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(Id),
        CONSTRAINT FK_Subscriptions_PlanId FOREIGN KEY (PlanId) REFERENCES dbo.Plans(Id),
        CONSTRAINT CK_Subscriptions_Status CHECK (Status IN ('Active', 'PastDue', 'Cancelled', 'Trialing')),
        CONSTRAINT CK_Subscriptions_BillingPeriod CHECK (BillingPeriod IN ('Monthly', 'Yearly'))
    );

    CREATE INDEX IX_Subscriptions_TenantId ON dbo.Subscriptions (TenantId);
    CREATE INDEX IX_Subscriptions_Status ON dbo.Subscriptions (Status);
END
