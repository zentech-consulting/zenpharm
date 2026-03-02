using Api.Common;
using Dapper;

namespace Api.Features.Reports;

internal sealed class ReportManager(
    ITenantDb db,
    ILogger<ReportManager> logger) : IReportManager
{
    public async Task<DashboardSummary> GetDashboardSummaryAsync(DateOnly? from, DateOnly? to, CancellationToken ct = default)
    {
        using var conn = await db.CreateAsync();

        var totalClients = await conn.ExecuteScalarAsync<int>(
            new CommandDefinition("SELECT COUNT(*) FROM dbo.Clients", cancellationToken: ct));

        var totalEmployees = await conn.ExecuteScalarAsync<int>(
            new CommandDefinition("SELECT COUNT(*) FROM dbo.Employees WHERE IsActive = 1", cancellationToken: ct));

        var bookingConditions = new List<string>();
        if (from.HasValue)
            bookingConditions.Add("CAST(b.StartTime AS DATE) >= @From");
        if (to.HasValue)
            bookingConditions.Add("CAST(b.StartTime AS DATE) <= @To");

        var bookingWhere = bookingConditions.Count > 0
            ? "WHERE " + string.Join(" AND ", bookingConditions)
            : "";

        var parameters = new { From = from, To = to };

        var totalBookings = await conn.ExecuteScalarAsync<int>(
            new CommandDefinition(
                $"SELECT COUNT(*) FROM dbo.Bookings b {bookingWhere}",
                parameters, cancellationToken: ct));

        var revenueWhere = bookingConditions.Count > 0
            ? bookingWhere + " AND b.Status = 'completed'"
            : "WHERE b.Status = 'completed'";

        var revenue = await conn.ExecuteScalarAsync<decimal?>(
            new CommandDefinition(
                $"""
                SELECT ISNULL(SUM(s.Price), 0)
                FROM dbo.Bookings b
                INNER JOIN dbo.Services s ON s.Id = b.ServiceId
                {revenueWhere}
                """,
                parameters, cancellationToken: ct)) ?? 0m;

        var dailyStatsSql = $"""
            SELECT CAST(b.StartTime AS DATE) AS Date,
                   COUNT(*) AS BookingCount,
                   ISNULL(SUM(CASE WHEN b.Status = 'completed' THEN s.Price ELSE 0 END), 0) AS Revenue
            FROM dbo.Bookings b
            INNER JOIN dbo.Services s ON s.Id = b.ServiceId
            {bookingWhere}
            GROUP BY CAST(b.StartTime AS DATE)
            ORDER BY CAST(b.StartTime AS DATE)
            """;

        var dailyStats = await conn.QueryAsync<DailyStat>(
            new CommandDefinition(dailyStatsSql, parameters, cancellationToken: ct));

        int totalProducts = 0, lowStockCount = 0, expiringCount = 0;
        try
        {
            totalProducts = await conn.ExecuteScalarAsync<int>(
                new CommandDefinition("SELECT COUNT(*) FROM dbo.TenantProducts", cancellationToken: ct));
            lowStockCount = await conn.ExecuteScalarAsync<int>(
                new CommandDefinition("SELECT COUNT(*) FROM dbo.TenantProducts WHERE StockQuantity <= ReorderLevel", cancellationToken: ct));
            expiringCount = await conn.ExecuteScalarAsync<int>(
                new CommandDefinition("SELECT COUNT(*) FROM dbo.TenantProducts WHERE ExpiryDate IS NOT NULL AND ExpiryDate <= DATEADD(DAY, 30, GETUTCDATE())", cancellationToken: ct));
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Inventory stats unavailable — TenantProducts table may not exist yet");
        }

        logger.LogInformation("Dashboard summary generated: {Clients} clients, {Bookings} bookings, {Revenue} revenue, {Products} products",
            totalClients, totalBookings, revenue, totalProducts);

        return new DashboardSummary(totalClients, totalBookings, totalEmployees, revenue, totalProducts, lowStockCount, expiringCount, dailyStats.ToList());
    }

    public async Task<TopSellingProductsReport> GetTopSellingProductsAsync(DateOnly? from, DateOnly? to, int limit, CancellationToken ct = default)
    {
        using var conn = await db.CreateAsync();

        var conditions = new List<string> { "sm.MovementType = 'stock_out'" };
        if (from.HasValue) conditions.Add("CAST(sm.CreatedAt AS DATE) >= @From");
        if (to.HasValue) conditions.Add("CAST(sm.CreatedAt AS DATE) <= @To");
        var whereClause = "WHERE " + string.Join(" AND ", conditions);

        var totalMovements = await conn.ExecuteScalarAsync<int>(
            new CommandDefinition(
                $"SELECT COUNT(*) FROM dbo.StockMovements sm {whereClause}",
                new { From = from, To = to }, cancellationToken: ct));

        var sql = $"""
            SELECT TOP(@Limit)
                tp.Id AS ProductId,
                ISNULL(tp.CustomName, tp.MasterProductName) AS ProductName,
                tp.Brand,
                tp.Category,
                SUM(sm.Quantity) AS TotalSold,
                SUM(sm.Quantity * ISNULL(tp.CustomPrice, tp.DefaultPrice)) AS TotalRevenue
            FROM dbo.StockMovements sm
            INNER JOIN dbo.TenantProducts tp ON tp.Id = sm.TenantProductId
            {whereClause}
            GROUP BY tp.Id, tp.CustomName, tp.MasterProductName, tp.Brand, tp.Category, tp.CustomPrice, tp.DefaultPrice
            ORDER BY SUM(sm.Quantity) DESC
            """;

        var items = await conn.QueryAsync<TopSellingProductDto>(
            new CommandDefinition(sql, new { From = from, To = to, Limit = limit }, cancellationToken: ct));

        return new TopSellingProductsReport(items.ToList(), totalMovements);
    }

    public async Task<RevenueByCategoryReport> GetRevenueByCategoryAsync(DateOnly? from, DateOnly? to, CancellationToken ct = default)
    {
        using var conn = await db.CreateAsync();

        var conditions = new List<string>();
        if (from.HasValue) conditions.Add("CAST(b.StartTime AS DATE) >= @From");
        if (to.HasValue) conditions.Add("CAST(b.StartTime AS DATE) <= @To");
        conditions.Add("b.Status = 'completed'");
        var whereClause = "WHERE " + string.Join(" AND ", conditions);

        var sql = $"""
            SELECT s.Category,
                   COUNT(*) AS BookingCount,
                   SUM(s.Price) AS Revenue
            FROM dbo.Bookings b
            INNER JOIN dbo.Services s ON s.Id = b.ServiceId
            {whereClause}
            GROUP BY s.Category
            ORDER BY SUM(s.Price) DESC
            """;

        var items = await conn.QueryAsync<RevenueByCategoryDto>(
            new CommandDefinition(sql, new { From = from, To = to }, cancellationToken: ct));

        var itemList = items.ToList();
        var totalRevenue = itemList.Sum(i => i.Revenue);

        return new RevenueByCategoryReport(itemList, totalRevenue);
    }

    public async Task<ExpiryWasteReport> GetExpiryWasteAsync(DateOnly? from, DateOnly? to, CancellationToken ct = default)
    {
        using var conn = await db.CreateAsync();

        var conditions = new List<string> { "sm.MovementType = 'expired'" };
        if (from.HasValue) conditions.Add("CAST(sm.CreatedAt AS DATE) >= @From");
        if (to.HasValue) conditions.Add("CAST(sm.CreatedAt AS DATE) <= @To");
        var whereClause = "WHERE " + string.Join(" AND ", conditions);

        var sql = $"""
            SELECT tp.Id AS ProductId,
                   ISNULL(tp.CustomName, tp.MasterProductName) AS ProductName,
                   tp.Brand,
                   SUM(sm.Quantity) AS ExpiredQuantity,
                   SUM(sm.Quantity * ISNULL(tp.CustomPrice, tp.DefaultPrice)) AS EstimatedWasteValue
            FROM dbo.StockMovements sm
            INNER JOIN dbo.TenantProducts tp ON tp.Id = sm.TenantProductId
            {whereClause}
            GROUP BY tp.Id, tp.CustomName, tp.MasterProductName, tp.Brand, tp.CustomPrice, tp.DefaultPrice
            ORDER BY SUM(sm.Quantity * ISNULL(tp.CustomPrice, tp.DefaultPrice)) DESC
            """;

        var items = await conn.QueryAsync<ExpiryWasteDto>(
            new CommandDefinition(sql, new { From = from, To = to }, cancellationToken: ct));

        var itemList = items.ToList();
        var totalMovements = itemList.Sum(i => i.ExpiredQuantity);
        var totalWaste = itemList.Sum(i => i.EstimatedWasteValue);

        return new ExpiryWasteReport(itemList, totalMovements, totalWaste);
    }

    public async Task<EmployeeUtilisationReport> GetEmployeeUtilisationAsync(DateOnly? from, DateOnly? to, CancellationToken ct = default)
    {
        using var conn = await db.CreateAsync();

        var conditions = new List<string> { "b.EmployeeId IS NOT NULL" };
        if (from.HasValue) conditions.Add("CAST(b.StartTime AS DATE) >= @From");
        if (to.HasValue) conditions.Add("CAST(b.StartTime AS DATE) <= @To");
        var whereClause = "WHERE " + string.Join(" AND ", conditions);

        var sql = $"""
            SELECT e.Id AS EmployeeId,
                   e.FirstName + ' ' + e.LastName AS EmployeeName,
                   e.Role,
                   COUNT(*) AS TotalBookings,
                   SUM(CASE WHEN b.Status = 'completed' THEN 1 ELSE 0 END) AS CompletedBookings,
                   ISNULL(SUM(CASE WHEN b.Status = 'completed' THEN s.Price ELSE 0 END), 0) AS Revenue
            FROM dbo.Bookings b
            INNER JOIN dbo.Employees e ON e.Id = b.EmployeeId
            INNER JOIN dbo.Services s ON s.Id = b.ServiceId
            {whereClause}
            GROUP BY e.Id, e.FirstName, e.LastName, e.Role
            ORDER BY COUNT(*) DESC
            """;

        var items = await conn.QueryAsync<EmployeeUtilisationDto>(
            new CommandDefinition(sql, new { From = from, To = to }, cancellationToken: ct));

        var itemList = items.ToList();
        var totalBookings = itemList.Sum(i => i.TotalBookings);

        return new EmployeeUtilisationReport(itemList, totalBookings);
    }
}
