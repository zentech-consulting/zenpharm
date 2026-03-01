-- Catalog DB: Development seed data
-- Creates Basic + Premium plans, a dev tenant, and links them via subscription.
-- Safe to run multiple times (uses IF NOT EXISTS checks).

-- Plans
IF NOT EXISTS (SELECT 1 FROM dbo.Plans WHERE Name = 'Basic')
BEGIN
    INSERT INTO dbo.Plans (Id, Name, PriceMonthly, PriceYearly, Features, MaxUsers, MaxProducts)
    VALUES (
        'A0000000-0000-0000-0000-000000000001',
        'Basic',
        49.00,
        490.00,
        '["dashboard","clients","bookings","products"]',
        5,
        500
    );
END

IF NOT EXISTS (SELECT 1 FROM dbo.Plans WHERE Name = 'Premium')
BEGIN
    INSERT INTO dbo.Plans (Id, Name, PriceMonthly, PriceYearly, Features, MaxUsers, MaxProducts)
    VALUES (
        'A0000000-0000-0000-0000-000000000002',
        'Premium',
        99.00,
        990.00,
        '["dashboard","clients","bookings","products","ai_chat","knowledge","notifications","reports","online_store"]',
        20,
        5000
    );
END

-- Dev tenant (uses DefaultConnection — same DB for simplicity in dev)
IF NOT EXISTS (SELECT 1 FROM dbo.Tenants WHERE Subdomain = 'dev')
BEGIN
    INSERT INTO dbo.Tenants (Id, Subdomain, DisplayName, PrimaryColour, ContactEmail, ConnectionString, Status)
    VALUES (
        'B0000000-0000-0000-0000-000000000001',
        'dev',
        'Dev Tenant',
        '#1890ff',
        'dev@zentech.com.au',
        '',  -- Set via user-secrets in dev; in prod, provisioning pipeline fills this
        'Active'
    );
END

-- Link dev tenant to Premium plan
IF NOT EXISTS (SELECT 1 FROM dbo.Subscriptions WHERE TenantId = 'B0000000-0000-0000-0000-000000000001')
BEGIN
    INSERT INTO dbo.Subscriptions (TenantId, PlanId, PlanName, Status, BillingPeriod)
    VALUES (
        'B0000000-0000-0000-0000-000000000001',
        'A0000000-0000-0000-0000-000000000002',
        'Premium',
        'Active',
        'Monthly'
    );
END
