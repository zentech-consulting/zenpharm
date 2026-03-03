using Api.Common.Migrations;
using Api.Common.Security;
using Dapper;

namespace Api.Common.Seeding;

internal sealed class DevSeedService(
    ICatalogDb catalogDb,
    IConnectionStringProtector protector,
    ITenantMigration tenantMigration,
    IConfiguration configuration,
    ILogger<DevSeedService> logger) : IDevSeedService
{
    public async Task SeedAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Dev seed starting...");

        var tenantConnString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(tenantConnString))
        {
            logger.LogWarning("Dev seed skipped — DefaultConnection not configured");
            return;
        }

        var subdomain = configuration["Tenancy:DevTenantSubdomain"] ?? "dev";

        try
        {
            using var conn = await catalogDb.CreateAsync();

            // 1. Seed plans (Basic + Premium)
            var basicPlanId = await SeedPlanAsync(conn, "Basic", 79.00m, 790.00m,
                """{"products":500,"users":5,"support":"email"}""", 5, 500, 5, ct);
            var premiumPlanId = await SeedPlanAsync(conn, "Premium", 199.00m, 1990.00m,
                """{"products":2000,"users":20,"support":"priority","analytics":true,"ai":"advanced"}""", 20, 2000, 20, ct);

            if (basicPlanId is null || premiumPlanId is null)
            {
                logger.LogWarning("Dev seed: could not seed plans — skipping remaining steps");
                return;
            }

            // 2. Seed master products (shared across all tenants)
            await SeedMasterProductsAsync(conn, ct);

            // 3. Seed dev tenant (Basic plan)
            var encryptedConnString = protector.Protect(tenantConnString);
            var devTenantId = await SeedTenantAsync(conn, subdomain, "Dev Pharmacy",
                encryptedConnString, "dev@zenpharm.local", ct);

            if (devTenantId is not null)
            {
                await SeedSubscriptionAsync(conn, devTenantId.Value, basicPlanId.Value, "Basic", ct);
                await tenantMigration.RunAllAsync(tenantConnString, ct);
                await SeedTenantDataAsync(tenantConnString, isDemoRich: true, ct);
                logger.LogInformation("Dev seed: Basic tenant '{Subdomain}' seeded", subdomain);
            }

            // 4. Seed premium demo tenant (same DB for dev, separate in production)
            var premiumSubdomain = "premium-demo";
            var premiumTenantId = await SeedTenantAsync(conn, premiumSubdomain, "Premium Demo Pharmacy",
                encryptedConnString, "premium@zenpharm.local", ct);

            if (premiumTenantId is not null)
            {
                await SeedSubscriptionAsync(conn, premiumTenantId.Value, premiumPlanId.Value, "Premium", ct);
                // Same DB in dev — migrations already ran; just seed extra data
                await SeedPremiumTenantDataAsync(tenantConnString, ct);
                logger.LogInformation("Dev seed: Premium tenant '{Subdomain}' seeded", premiumSubdomain);
            }

            logger.LogInformation("Dev seed complete");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Dev seed failed — the application will continue without seed data");
        }
    }

    // ─── Catalogue DB Seed Methods ───────────────────────────────────────

    private static async Task<Guid?> SeedPlanAsync(
        System.Data.IDbConnection conn, string name,
        decimal priceMonthly, decimal priceYearly, string features,
        int maxUsers, int maxProducts, int maxConcurrentSessions,
        CancellationToken ct)
    {
        const string sql = """
            IF NOT EXISTS (SELECT 1 FROM dbo.Plans WHERE Name = @Name)
            BEGIN
                DECLARE @PlanId TABLE(Id UNIQUEIDENTIFIER);
                INSERT INTO dbo.Plans (Name, PriceMonthly, PriceYearly, Features, MaxUsers, MaxProducts, MaxConcurrentSessions)
                OUTPUT INSERTED.Id INTO @PlanId
                VALUES (@Name, @PriceMonthly, @PriceYearly, @Features, @MaxUsers, @MaxProducts, @MaxConcurrentSessions);
                SELECT Id FROM @PlanId;
            END
            ELSE
            BEGIN
                SELECT Id FROM dbo.Plans WHERE Name = @Name;
            END
            """;

        return await conn.QuerySingleOrDefaultAsync<Guid?>(
            new CommandDefinition(sql, new { Name = name, PriceMonthly = priceMonthly, PriceYearly = priceYearly, Features = features, MaxUsers = maxUsers, MaxProducts = maxProducts, MaxConcurrentSessions = maxConcurrentSessions }, cancellationToken: ct));
    }

    private static async Task<Guid?> SeedTenantAsync(
        System.Data.IDbConnection conn, string subdomain, string displayName,
        string connectionString, string contactEmail, CancellationToken ct)
    {
        const string sql = """
            IF NOT EXISTS (SELECT 1 FROM dbo.Tenants WHERE Subdomain = @Subdomain)
            BEGIN
                DECLARE @TenantId TABLE(Id UNIQUEIDENTIFIER);
                INSERT INTO dbo.Tenants (Subdomain, DisplayName, ConnectionString, ContactEmail)
                OUTPUT INSERTED.Id INTO @TenantId
                VALUES (@Subdomain, @DisplayName, @ConnectionString, @ContactEmail);
                SELECT Id FROM @TenantId;
            END
            ELSE
            BEGIN
                SELECT Id FROM dbo.Tenants WHERE Subdomain = @Subdomain;
            END
            """;

        return await conn.QuerySingleOrDefaultAsync<Guid?>(
            new CommandDefinition(sql, new { Subdomain = subdomain, DisplayName = displayName, ConnectionString = connectionString, ContactEmail = contactEmail }, cancellationToken: ct));
    }

    private static async Task SeedSubscriptionAsync(
        System.Data.IDbConnection conn, Guid tenantId, Guid planId, string planName, CancellationToken ct)
    {
        const string sql = """
            IF NOT EXISTS (SELECT 1 FROM dbo.Subscriptions WHERE TenantId = @TenantId)
            BEGIN
                INSERT INTO dbo.Subscriptions (TenantId, PlanId, PlanName, Status, BillingPeriod)
                VALUES (@TenantId, @PlanId, @PlanName, 'Active', 'Monthly');
            END
            """;

        await conn.ExecuteAsync(
            new CommandDefinition(sql, new { TenantId = tenantId, PlanId = planId, PlanName = planName }, cancellationToken: ct));
    }

    private static async Task SeedMasterProductsAsync(
        System.Data.IDbConnection conn, CancellationToken ct)
    {
        const string sql = """
            IF NOT EXISTS (SELECT 1 FROM dbo.MasterProducts WHERE Sku = @Sku)
            BEGIN
                INSERT INTO dbo.MasterProducts
                    (Sku, Name, Category, Description, UnitPrice, GenericName, Brand, Barcode,
                     ScheduleClass, PackSize, ActiveIngredients, Warnings)
                VALUES
                    (@Sku, @Name, @Category, @Description, @UnitPrice, @GenericName, @Brand, @Barcode,
                     @ScheduleClass, @PackSize, @ActiveIngredients, @Warnings);
            END
            """;

        foreach (var product in PharmacyMasterProductData.All)
        {
            await conn.ExecuteAsync(new CommandDefinition(sql, new
            {
                product.Sku,
                product.Name,
                product.Category,
                product.Description,
                product.UnitPrice,
                product.GenericName,
                product.Brand,
                product.Barcode,
                product.ScheduleClass,
                product.PackSize,
                product.ActiveIngredients,
                product.Warnings
            }, cancellationToken: ct));
        }
    }

    // ─── Tenant DB Seed Methods ──────────────────────────────────────────

    private static async Task SeedTenantDataAsync(string connectionString, bool isDemoRich, CancellationToken ct)
    {
        await using var conn = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
        await conn.OpenAsync(ct);

        // Admin users
        await SeedAdminUsersAsync(conn, ct);

        if (!isDemoRich) return;

        // Employees
        var employeeIds = await SeedEmployeesAsync(conn, ct);

        // Clients
        var clientIds = await SeedClientsAsync(conn, ct);

        // Services
        var serviceIds = await SeedServicesAsync(conn, ct);

        // Schedules (need employee IDs)
        await SeedSchedulesAsync(conn, employeeIds, ct);

        // Bookings (need client, service, employee IDs)
        await SeedBookingsAsync(conn, clientIds, serviceIds, employeeIds, ct);

        // Import products + stock
        await SeedTenantProductsAsync(conn, ct);

        // Sample orders for shop demo
        await SeedOrdersAsync(conn, clientIds, ct);

        // Knowledge entries for AI
        await SeedKnowledgeEntriesAsync(conn, ct);
    }

    private static async Task SeedPremiumTenantDataAsync(string connectionString, CancellationToken ct)
    {
        await using var conn = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
        await conn.OpenAsync(ct);

        // Premium gets a separate admin account
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("premium123");
        const string sql = """
            IF NOT EXISTS (SELECT 1 FROM dbo.AdminUsers WHERE Username = 'premium-admin')
            BEGIN
                INSERT INTO dbo.AdminUsers (Username, Email, PasswordHash, FullName, Role)
                VALUES ('premium-admin', 'premium-admin@zenpharm.local', @PasswordHash, 'Premium Admin', 'SuperAdmin');
            END
            """;
        await conn.ExecuteAsync(new CommandDefinition(sql, new { PasswordHash = passwordHash }, cancellationToken: ct));

        // Additional manager account for Premium
        var mgrHash = BCrypt.Net.BCrypt.HashPassword("manager123");
        const string mgrSql = """
            IF NOT EXISTS (SELECT 1 FROM dbo.AdminUsers WHERE Username = 'premium-manager')
            BEGIN
                INSERT INTO dbo.AdminUsers (Username, Email, PasswordHash, FullName, Role)
                VALUES ('premium-manager', 'manager@zenpharm.local', @PasswordHash, 'Sarah Chen', 'Manager');
            END
            """;
        await conn.ExecuteAsync(new CommandDefinition(mgrSql, new { PasswordHash = mgrHash }, cancellationToken: ct));
    }

    private static async Task SeedAdminUsersAsync(System.Data.Common.DbConnection conn, CancellationToken ct)
    {
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("admin123");

        const string sql = """
            IF NOT EXISTS (SELECT 1 FROM dbo.AdminUsers WHERE Username = 'admin')
            BEGIN
                INSERT INTO dbo.AdminUsers (Username, Email, PasswordHash, FullName, Role)
                VALUES ('admin', 'admin@zenpharm.local', @PasswordHash, 'Dev Admin', 'SuperAdmin');
            END
            """;
        await conn.ExecuteAsync(new CommandDefinition(sql, new { PasswordHash = passwordHash }, cancellationToken: ct));

        // Staff user for demo
        var staffHash = BCrypt.Net.BCrypt.HashPassword("staff123");
        const string staffSql = """
            IF NOT EXISTS (SELECT 1 FROM dbo.AdminUsers WHERE Username = 'staff')
            BEGIN
                INSERT INTO dbo.AdminUsers (Username, Email, PasswordHash, FullName, Role)
                VALUES ('staff', 'staff@zenpharm.local', @PasswordHash, 'Demo Staff', 'Staff');
            END
            """;
        await conn.ExecuteAsync(new CommandDefinition(staffSql, new { PasswordHash = staffHash }, cancellationToken: ct));
    }

    private static async Task<Guid[]> SeedEmployeesAsync(System.Data.Common.DbConnection conn, CancellationToken ct)
    {
        var employees = new[]
        {
            ("James", "Mitchell",  "james.mitchell@zenpharm.local",  "0412345001", "pharmacist"),
            ("Emily", "Nguyen",    "emily.nguyen@zenpharm.local",    "0412345002", "pharmacist"),
            ("David", "Thompson",  "david.thompson@zenpharm.local",  "0412345003", "dispense_technician"),
            ("Sophie", "Williams", "sophie.williams@zenpharm.local", "0412345004", "pharmacy_assistant"),
            ("Michael", "Brown",   "michael.brown@zenpharm.local",   "0412345005", "cashier"),
            ("Rachel", "Lee",      "rachel.lee@zenpharm.local",      "0412345006", "manager"),
        };

        var ids = new List<Guid>();
        const string sql = """
            IF NOT EXISTS (SELECT 1 FROM dbo.Employees WHERE Email = @Email)
            BEGIN
                DECLARE @EmpId TABLE(Id UNIQUEIDENTIFIER);
                INSERT INTO dbo.Employees (FirstName, LastName, Email, Phone, Role)
                OUTPUT INSERTED.Id INTO @EmpId
                VALUES (@FirstName, @LastName, @Email, @Phone, @Role);
                SELECT Id FROM @EmpId;
            END
            ELSE
            BEGIN
                SELECT Id FROM dbo.Employees WHERE Email = @Email;
            END
            """;

        foreach (var (first, last, email, phone, role) in employees)
        {
            var id = await conn.QuerySingleOrDefaultAsync<Guid?>(
                new CommandDefinition(sql, new { FirstName = first, LastName = last, Email = email, Phone = phone, Role = role }, cancellationToken: ct));
            if (id.HasValue) ids.Add(id.Value);
        }

        return ids.ToArray();
    }

    private static async Task<Guid[]> SeedClientsAsync(System.Data.Common.DbConnection conn, CancellationToken ct)
    {
        var clients = new (string First, string Last, string? Email, string? Phone, string? DateOfBirth, string? Allergies, string? MedicationNotes, string? Tags)[]
        {
            ("Margaret", "Wilson",    "margaret.wilson@email.com",   "0400111001", "1952-03-15", "Penicillin",                  "Warfarin 5mg daily, Metformin 500mg twice daily",        "elderly,regular"),
            ("Robert",   "Taylor",    "robert.taylor@email.com",    "0400111002", "1968-07-22", null,                          "Atorvastatin 40mg daily",                                 "regular"),
            ("Susan",    "Anderson",  "susan.anderson@email.com",   "0400111003", "1975-11-08", "Sulfonamides",                "Fluoxetine 20mg daily",                                   "mental-health"),
            ("John",     "Martin",    "john.martin@email.com",      "0400111004", "1945-01-30", "Codeine, Aspirin",            "Amlodipine 10mg, Perindopril 5mg, Rosuvastatin 20mg",    "elderly,cardiovascular,regular"),
            ("Patricia", "Clark",     "patricia.clark@email.com",   "0400111005", "1988-05-17", null,                          null,                                                       "young-family"),
            ("Thomas",   "Wright",    "thomas.wright@email.com",    "0400111006", "1960-09-03", "Ibuprofen",                   "Metoprolol 100mg daily, Warfarin 3mg daily",              "cardiovascular"),
            ("Jennifer", "Harris",    "jennifer.harris@email.com",  "0400111007", "1992-12-25", null,                          "Ventolin PRN",                                             "asthma"),
            ("William",  "Walker",    "william.walker@email.com",   "0400111008", "1955-06-14", null,                          "Insulin Glargine 20 units, Metformin 1000mg twice daily", "diabetes,regular"),
            ("Linda",    "Robinson",  "linda.robinson@email.com",   "0400111009", "1970-04-19", "Latex",                       "Thyroxine 100mcg daily",                                  "thyroid"),
            ("David",    "Hall",      "david.hall@email.com",       "0400111010", "1983-08-07", null,                          null,                                                       null),
            ("Karen",    "Young",     "karen.young@email.com",      "0400111011", "1998-02-28", "Peanuts",                     null,                                                       "allergy"),
            ("Richard",  "King",      "richard.king@email.com",     "0400111012", "1940-10-11", "Morphine, Tramadol",          "Pantoprazole 40mg, Paracetamol PRN",                      "elderly,pain-management"),
            ("Lisa",     "Scott",     "lisa.scott@email.com",       "0400111013", "1985-07-02", null,                          "Oral contraceptive pill",                                  "womens-health"),
            ("James",    "Green",     null,                         "0400111014", "1978-03-20", null,                          "Salbutamol PRN, Seretide 250/25",                         "asthma"),
            ("Mary",     "Baker",     "mary.baker@email.com",       "0400111015", "1963-11-30", "Erythromycin",                "Alendronate 70mg weekly, Calcium + Vitamin D",            "osteoporosis,elderly"),
        };

        var ids = new List<Guid>();
        const string sql = """
            IF NOT EXISTS (SELECT 1 FROM dbo.Clients WHERE FirstName = @FirstName AND LastName = @LastName AND (Phone = @Phone OR (Phone IS NULL AND @Phone IS NULL)))
            BEGIN
                DECLARE @CId TABLE(Id UNIQUEIDENTIFIER);
                INSERT INTO dbo.Clients (FirstName, LastName, Email, Phone, DateOfBirth, Allergies, MedicationNotes, Tags)
                OUTPUT INSERTED.Id INTO @CId
                VALUES (@FirstName, @LastName, @Email, @Phone, @DateOfBirth, @Allergies, @MedicationNotes, @Tags);
                SELECT Id FROM @CId;
            END
            ELSE
            BEGIN
                SELECT TOP 1 Id FROM dbo.Clients WHERE FirstName = @FirstName AND LastName = @LastName;
            END
            """;

        foreach (var c in clients)
        {
            var id = await conn.QuerySingleOrDefaultAsync<Guid?>(
                new CommandDefinition(sql, new
                {
                    FirstName = c.First, LastName = c.Last, Email = c.Email, Phone = c.Phone,
                    DateOfBirth = c.DateOfBirth is not null ? DateOnly.Parse(c.DateOfBirth) : (DateOnly?)null,
                    Allergies = c.Allergies, MedicationNotes = c.MedicationNotes, Tags = c.Tags
                }, cancellationToken: ct));
            if (id.HasValue) ids.Add(id.Value);
        }

        return ids.ToArray();
    }

    private static async Task<Guid[]> SeedServicesAsync(System.Data.Common.DbConnection conn, CancellationToken ct)
    {
        var services = new (string Name, string Description, string Category, decimal Price, int Duration)[]
        {
            ("Medication Review",          "Comprehensive review of current medications for interactions and optimisation", "Consultation", 45.00m,  30),
            ("Blood Pressure Check",       "Accurate blood pressure measurement with lifestyle advice",                    "Health Check",  15.00m,  15),
            ("Blood Glucose Monitoring",   "Fasting or random blood glucose level check",                                  "Health Check",  20.00m,  15),
            ("Flu Vaccination",            "Annual influenza vaccination by qualified pharmacist",                          "Vaccination",   25.00m,  15),
            ("COVID-19 Vaccination",       "COVID-19 booster vaccination",                                                 "Vaccination",   0.00m,   20),
            ("Diabetes Management Review", "Comprehensive diabetes care plan review and HbA1c discussion",                 "Consultation", 55.00m,  45),
            ("Wound Care",                 "Minor wound cleaning, dressing, and aftercare advice",                          "First Aid",     30.00m,  20),
            ("Prescription Dispensing",    "Standard prescription dispensing with counselling",                              "Dispensing",    10.00m,  10),
            ("Weight Management Consult",  "Weight management advice with BMI assessment and goal setting",                 "Consultation", 40.00m,  30),
            ("Asthma Device Training",     "Inhaler technique review and device training",                                 "Education",     0.00m,   20),
        };

        var ids = new List<Guid>();
        const string sql = """
            IF NOT EXISTS (SELECT 1 FROM dbo.Services WHERE Name = @Name)
            BEGIN
                DECLARE @SId TABLE(Id UNIQUEIDENTIFIER);
                INSERT INTO dbo.Services (Name, Description, Category, Price, DurationMinutes)
                OUTPUT INSERTED.Id INTO @SId
                VALUES (@Name, @Description, @Category, @Price, @DurationMinutes);
                SELECT Id FROM @SId;
            END
            ELSE
            BEGIN
                SELECT Id FROM dbo.Services WHERE Name = @Name;
            END
            """;

        foreach (var (name, desc, cat, price, dur) in services)
        {
            var id = await conn.QuerySingleOrDefaultAsync<Guid?>(
                new CommandDefinition(sql, new { Name = name, Description = desc, Category = cat, Price = price, DurationMinutes = dur }, cancellationToken: ct));
            if (id.HasValue) ids.Add(id.Value);
        }

        return ids.ToArray();
    }

    private static async Task SeedSchedulesAsync(
        System.Data.Common.DbConnection conn, Guid[] employeeIds, CancellationToken ct)
    {
        if (employeeIds.Length == 0) return;

        // Seed schedules for the next 14 days
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        const string sql = """
            IF NOT EXISTS (SELECT 1 FROM dbo.Schedules WHERE EmployeeId = @EmployeeId AND Date = @Date)
            BEGIN
                INSERT INTO dbo.Schedules (EmployeeId, Date, StartTime, EndTime, Location)
                VALUES (@EmployeeId, @Date, @StartTime, @EndTime, @Location);
            END
            """;

        for (var dayOffset = 0; dayOffset < 14; dayOffset++)
        {
            var date = today.AddDays(dayOffset);
            if (date.DayOfWeek is DayOfWeek.Sunday) continue;

            foreach (var empId in employeeIds)
            {
                // Saturday = half day, weekdays = full day
                var (start, end) = date.DayOfWeek == DayOfWeek.Saturday
                    ? (new TimeOnly(8, 30), new TimeOnly(13, 0))
                    : (new TimeOnly(8, 30), new TimeOnly(17, 30));

                await conn.ExecuteAsync(new CommandDefinition(sql, new
                {
                    EmployeeId = empId, Date = date,
                    StartTime = start, EndTime = end,
                    Location = "Main Pharmacy"
                }, cancellationToken: ct));
            }
        }
    }

    private static async Task SeedBookingsAsync(
        System.Data.Common.DbConnection conn, Guid[] clientIds, Guid[] serviceIds, Guid[] employeeIds,
        CancellationToken ct)
    {
        if (clientIds.Length == 0 || serviceIds.Length == 0 || employeeIds.Length == 0) return;

        const string sql = """
            IF NOT EXISTS (SELECT 1 FROM dbo.Bookings WHERE ClientId = @ClientId AND StartTime = @StartTime)
            BEGIN
                INSERT INTO dbo.Bookings (ClientId, ServiceId, EmployeeId, StartTime, EndTime, Status, Notes)
                VALUES (@ClientId, @ServiceId, @EmployeeId, @StartTime, @EndTime, @Status, @Notes);
            END
            """;

        var now = DateTimeOffset.UtcNow;
        var pharmacists = employeeIds.Take(2).ToArray(); // First 2 are pharmacists

        var bookings = new (int ClientIdx, int ServiceIdx, int EmpIdx, int DayOffset, int Hour, int Min, string Status, string? Notes)[]
        {
            // Past bookings (completed/no_show)
            (0,  0, 0, -7,  9, 0,  "completed", "Regular medication review — all good"),
            (1,  1, 1, -7, 10, 0,  "completed", "BP: 130/85 — advised lifestyle changes"),
            (3,  5, 0, -6, 14, 0,  "completed", "HbA1c reviewed, adjusting insulin dose"),
            (2,  7, 1, -5, 11, 0,  "completed", null),
            (4,  3, 0, -4, 15, 0,  "completed", "Flu shot administered, no adverse reaction"),
            (7,  2, 1, -3,  9, 30, "completed", "Fasting glucose: 7.2 mmol/L — monitoring"),
            (5,  0, 0, -3, 11, 0,  "completed", "Reviewed warfarin interactions"),
            (11, 6, 1, -2, 10, 0,  "completed", "Minor laceration cleaned and dressed"),
            (8,  7, 0, -2, 14, 0,  "no_show",   null),
            (9,  1, 1, -1, 16, 0,  "completed", "BP: 120/80 — within normal range"),
            // Today's bookings
            (0,  7, 0,  0, 10, 0,  "confirmed", "Monthly prescription refill"),
            (6,  9, 0,  0, 11, 0,  "confirmed", "Inhaler technique check — Ventolin"),
            (3,  2, 1,  0, 14, 0,  "pending",   null),
            (10, 3, 0,  0, 15, 30, "confirmed", "First flu vaccination"),
            // Future bookings
            (1,  0, 0,  1,  9, 0,  "confirmed", "Follow-up medication review"),
            (14, 0, 0,  2, 10, 0,  "pending",   "Osteoporosis medication review"),
            (7,  5, 0,  2, 14, 0,  "confirmed", "Quarterly diabetes check"),
            (4,  8, 1,  3, 11, 0,  "pending",   "Weight management initial consult"),
            (12, 7, 1,  4, 10, 0,  "confirmed", null),
            (13, 9, 0,  5,  9, 0,  "pending",   "Asthma device retraining"),
            (5,  1, 1,  5, 14, 0,  "confirmed", "Routine BP monitoring"),
            (2,  0, 0,  7, 10, 0,  "pending",   "Medication review follow-up"),
            (0,  7, 0,  7, 15, 0,  "cancelled", "Patient called to reschedule"),
        };

        foreach (var b in bookings)
        {
            var clientId = clientIds[b.ClientIdx % clientIds.Length];
            var serviceId = serviceIds[b.ServiceIdx % serviceIds.Length];
            var empId = pharmacists[b.EmpIdx % pharmacists.Length];
            var start = now.Date.AddDays(b.DayOffset).AddHours(b.Hour).AddMinutes(b.Min);
            var end = start.AddMinutes(30);

            await conn.ExecuteAsync(new CommandDefinition(sql, new
            {
                ClientId = clientId, ServiceId = serviceId, EmployeeId = empId,
                StartTime = new DateTimeOffset(start, TimeSpan.Zero),
                EndTime = new DateTimeOffset(end, TimeSpan.Zero),
                Status = b.Status, Notes = b.Notes
            }, cancellationToken: ct));
        }
    }

    private static async Task SeedTenantProductsAsync(System.Data.Common.DbConnection conn, CancellationToken ct)
    {
        // Import first 50 master products into tenant with realistic stock levels
        const string countSql = "SELECT COUNT(*) FROM dbo.TenantProducts";
        var existing = await conn.ExecuteScalarAsync<int>(new CommandDefinition(countSql, cancellationToken: ct));
        if (existing > 0) return; // Already seeded

        const string importSql = """
            INSERT INTO dbo.TenantProducts
                (MasterProductId, MasterProductName, GenericName, Brand, Category,
                 ScheduleClass, DefaultPrice, StockQuantity, ReorderLevel, ExpiryDate,
                 IsVisible, IsFeatured, SortOrder)
            SELECT TOP 50
                mp.Id, mp.Name, mp.GenericName, mp.Brand, mp.Category,
                mp.ScheduleClass, mp.UnitPrice,
                ABS(CHECKSUM(NEWID())) % 80 + 10,  -- Random stock: 10-89
                CASE mp.ScheduleClass
                    WHEN 'S4' THEN 5
                    WHEN 'S3' THEN 10
                    ELSE 15
                END,
                DATEADD(MONTH, ABS(CHECKSUM(NEWID())) % 24 + 3, SYSUTCDATETIME()), -- 3-27 months from now
                1,
                CASE WHEN ROW_NUMBER() OVER (ORDER BY mp.Category, mp.Name) <= 10 THEN 1 ELSE 0 END,
                ROW_NUMBER() OVER (ORDER BY mp.Category, mp.Name)
            FROM dbo.MasterProducts mp
            ORDER BY mp.Category, mp.Name
            """;

        await conn.ExecuteAsync(new CommandDefinition(importSql, cancellationToken: ct));

        // Record initial stock-in movements for all imported products
        const string stockMovementSql = """
            INSERT INTO dbo.StockMovements (TenantProductId, MovementType, Quantity, Reference, Notes, CreatedBy)
            SELECT tp.Id, 'stock_in', tp.StockQuantity, 'INIT-SEED', 'Initial stock from dev seed', 'System'
            FROM dbo.TenantProducts tp
            WHERE NOT EXISTS (
                SELECT 1 FROM dbo.StockMovements sm
                WHERE sm.TenantProductId = tp.Id AND sm.Reference = 'INIT-SEED'
            )
            """;

        await conn.ExecuteAsync(new CommandDefinition(stockMovementSql, cancellationToken: ct));

        // Set a few products as low stock for demo purposes
        const string lowStockSql = """
            UPDATE TOP (5) dbo.TenantProducts
            SET StockQuantity = ReorderLevel - 2
            WHERE StockQuantity > ReorderLevel
            """;
        await conn.ExecuteAsync(new CommandDefinition(lowStockSql, cancellationToken: ct));

        // Set 3 products with near-expiry dates
        const string expirySql = """
            UPDATE TOP (3) dbo.TenantProducts
            SET ExpiryDate = DATEADD(DAY, 14, SYSUTCDATETIME())
            WHERE ExpiryDate > DATEADD(MONTH, 6, SYSUTCDATETIME())
            """;
        await conn.ExecuteAsync(new CommandDefinition(expirySql, cancellationToken: ct));
    }

    private static async Task SeedOrdersAsync(
        System.Data.Common.DbConnection conn, Guid[] clientIds, CancellationToken ct)
    {
        if (clientIds.Length < 5) return;

        // Check if orders already seeded
        var existing = await conn.ExecuteScalarAsync<int>(
            new CommandDefinition("SELECT COUNT(*) FROM dbo.Orders", cancellationToken: ct));
        if (existing > 0) return;

        // Get some product IDs for order items
        var products = (await conn.QueryAsync<(Guid Id, string Name, decimal Price)>(
            new CommandDefinition(
                "SELECT TOP 10 Id, COALESCE(CustomName, MasterProductName) AS Name, COALESCE(CustomPrice, DefaultPrice) AS Price FROM dbo.TenantProducts WHERE IsVisible = 1 AND ScheduleClass != 'S4' ORDER BY SortOrder",
                cancellationToken: ct))).ToArray();

        if (products.Length < 3) return;

        var today = DateTime.UtcNow.ToString("yyyyMMdd");
        var orders = new (int ClientIdx, string Status, int DayOffset, (int ProdIdx, int Qty)[] Items, string? Notes, string? CancelReason)[]
        {
            (0, "pending",   0, [(0, 2), (1, 1)], "Please have ready by lunch", null),
            (1, "pending",   0, [(2, 1), (3, 3)], null, null),
            (2, "ready",    -1, [(0, 1), (4, 2)], null, null),
            (3, "collected", -3, [(1, 2)], "Picking up on the way home", null),
            (4, "cancelled", -2, [(2, 5), (0, 1)], null, "Customer changed their mind"),
        };

        var seq = 0;
        foreach (var o in orders)
        {
            seq++;
            var clientId = clientIds[o.ClientIdx % clientIds.Length];
            var orderDate = DateTime.UtcNow.AddDays(o.DayOffset).ToString("yyyyMMdd");
            var orderNumber = $"ORD-{orderDate}-{seq:D4}";

            decimal subtotal = 0;
            foreach (var (prodIdx, qty) in o.Items)
            {
                var prod = products[prodIdx % products.Length];
                subtotal += prod.Price * qty;
            }
            var taxAmount = Math.Round(subtotal * 0.10m, 2);
            var total = subtotal + taxAmount;

            var estimatedReady = DateTimeOffset.UtcNow.AddHours(2);

            // Insert order
            var insertSql = """
                DECLARE @OrdId TABLE(Id UNIQUEIDENTIFIER);
                INSERT INTO dbo.Orders (OrderNumber, ClientId, Status, Subtotal, TaxAmount, Total, Notes, EstimatedReadyAt, CancellationReason)
                OUTPUT INSERTED.Id INTO @OrdId
                VALUES (@OrderNumber, @ClientId, @Status, @Subtotal, @TaxAmount, @Total, @Notes, @EstimatedReadyAt, @CancellationReason);
                SELECT Id FROM @OrdId;
                """;

            var orderId = await conn.QuerySingleAsync<Guid>(
                new CommandDefinition(insertSql, new
                {
                    OrderNumber = orderNumber,
                    ClientId = clientId,
                    Status = o.Status,
                    Subtotal = subtotal,
                    TaxAmount = taxAmount,
                    Total = total,
                    Notes = o.Notes,
                    EstimatedReadyAt = estimatedReady,
                    CancellationReason = o.CancelReason
                }, cancellationToken: ct));

            // Set timestamps based on status
            if (o.Status is "ready" or "collected")
            {
                await conn.ExecuteAsync(new CommandDefinition(
                    "UPDATE dbo.Orders SET ReadyNotifiedAt = DATEADD(DAY, @Offset, SYSUTCDATETIME()) WHERE Id = @Id",
                    new { Offset = o.DayOffset, Id = orderId }, cancellationToken: ct));
            }
            if (o.Status == "collected")
            {
                await conn.ExecuteAsync(new CommandDefinition(
                    "UPDATE dbo.Orders SET CollectedAt = DATEADD(DAY, @Offset, SYSUTCDATETIME()) WHERE Id = @Id",
                    new { Offset = o.DayOffset, Id = orderId }, cancellationToken: ct));
            }
            if (o.Status == "cancelled")
            {
                await conn.ExecuteAsync(new CommandDefinition(
                    "UPDATE dbo.Orders SET CancelledAt = DATEADD(DAY, @Offset, SYSUTCDATETIME()) WHERE Id = @Id",
                    new { Offset = o.DayOffset, Id = orderId }, cancellationToken: ct));
            }

            // Insert order items
            foreach (var (prodIdx, qty) in o.Items)
            {
                var prod = products[prodIdx % products.Length];
                var itemSubtotal = prod.Price * qty;

                await conn.ExecuteAsync(new CommandDefinition(
                    """
                    INSERT INTO dbo.OrderItems (OrderId, TenantProductId, ProductName, Quantity, UnitPrice, Subtotal)
                    VALUES (@OrderId, @TenantProductId, @ProductName, @Quantity, @UnitPrice, @Subtotal)
                    """,
                    new
                    {
                        OrderId = orderId,
                        TenantProductId = prod.Id,
                        ProductName = prod.Name,
                        Quantity = qty,
                        UnitPrice = prod.Price,
                        Subtotal = itemSubtotal
                    }, cancellationToken: ct));
            }
        }
    }

    private static async Task SeedKnowledgeEntriesAsync(System.Data.Common.DbConnection conn, CancellationToken ct)
    {
        var entries = new (string Title, string Content, string Category, string Tags)[]
        {
            ("Opening Hours", "We are open Monday to Friday 8:30am to 5:30pm, and Saturday 8:30am to 1:00pm. Closed on Sundays and public holidays.", "General", "hours,opening,times"),
            ("Prescription Dispensing", "We dispense PBS and private prescriptions. Please allow 10-15 minutes for dispensing. Bring your Medicare card for PBS subsidies. We offer medication counselling with every prescription.", "Services", "prescription,pbs,dispensing"),
            ("Flu Vaccination Service", "We offer flu vaccinations by qualified pharmacists for adults aged 18+. No appointment needed during flu season (March-June), but booking is recommended at other times. Cost is $25 or free for eligible Medicare patients.", "Services", "flu,vaccination,immunisation"),
            ("Medication Reviews", "Our pharmacists provide comprehensive Home Medicines Reviews (HMR) and MedsCheck services. These are free under PBS for eligible patients. Ask your GP for a referral or speak with our pharmacist.", "Services", "medscheck,hmr,medication-review"),
            ("Diabetes Management", "We offer blood glucose monitoring, HbA1c education, insulin device training, and comprehensive diabetes management consultations. Our pharmacists can help you understand your diabetes medications and monitor your condition.", "Health", "diabetes,glucose,insulin,management"),
            ("Blood Pressure Monitoring", "Free blood pressure checks are available during pharmacy hours. We use calibrated automated monitors. Results are recorded on a tracking card. Speak with our pharmacist if your readings are outside normal range.", "Health", "blood-pressure,monitoring,cardiovascular"),
            ("Asthma Management", "We provide inhaler technique checks, spacer demonstrations, and Asthma Action Plan reviews. Good inhaler technique ensures you get the full benefit of your medication.", "Health", "asthma,inhaler,respiratory"),
            ("Prescription Delivery", "We offer free local delivery for prescriptions within a 5km radius. Deliveries are made Monday to Friday. Please call before 2pm for same-day delivery.", "Services", "delivery,prescription,local"),
            ("Returns Policy", "Medications cannot be returned once dispensed for safety reasons. Faulty or incorrect items will be replaced. Non-medication items may be returned within 14 days with receipt.", "Policy", "returns,refunds,policy"),
            ("Privacy and Health Records", "Your health information is stored securely and handled in accordance with the Australian Privacy Principles. We share information with your GP and other healthcare providers only with your consent.", "Policy", "privacy,records,health-information"),
        };

        const string sql = """
            IF NOT EXISTS (SELECT 1 FROM dbo.KnowledgeEntries WHERE Title = @Title)
            BEGIN
                INSERT INTO dbo.KnowledgeEntries (Title, Content, Category, Tags)
                VALUES (@Title, @Content, @Category, @Tags);
            END
            """;

        foreach (var (title, content, category, tags) in entries)
        {
            await conn.ExecuteAsync(new CommandDefinition(sql, new { Title = title, Content = content, Category = category, Tags = tags }, cancellationToken: ct));
        }
    }
}
