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
}
