using Api.Features.Reports;
using Xunit;

namespace Api.Tests.Reports;

public class ReportContractTests
{
    [Fact]
    public void DashboardSummary_RecordEquality()
    {
        var stats = new List<DailyStat>();
        var a = new DashboardSummary(10, 25, 5, 1500.00m, stats);
        var b = new DashboardSummary(10, 25, 5, 1500.00m, stats);

        Assert.Equal(a, b);
    }

    [Fact]
    public void DashboardSummary_DifferentRevenue_NotEqual()
    {
        var stats = new List<DailyStat>();
        var a = new DashboardSummary(10, 25, 5, 1500.00m, stats);
        var b = new DashboardSummary(10, 25, 5, 2000.00m, stats);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void DailyStat_RecordEquality()
    {
        var date = new DateOnly(2025, 6, 15);
        var a = new DailyStat(date, 5, 250.00m);
        var b = new DailyStat(date, 5, 250.00m);

        Assert.Equal(a, b);
    }

    [Fact]
    public void DailyStat_DifferentDate_NotEqual()
    {
        var a = new DailyStat(new DateOnly(2025, 6, 15), 5, 250.00m);
        var b = new DailyStat(new DateOnly(2025, 6, 16), 5, 250.00m);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void DashboardSummary_EmptyDailyStats()
    {
        var summary = new DashboardSummary(0, 0, 0, 0m, []);

        Assert.Equal(0, summary.TotalClients);
        Assert.Equal(0, summary.TotalBookings);
        Assert.Equal(0, summary.TotalEmployees);
        Assert.Equal(0m, summary.Revenue);
        Assert.Empty(summary.DailyStats);
    }

    [Fact]
    public void DashboardSummary_WithDailyStats()
    {
        var stats = new List<DailyStat>
        {
            new(new DateOnly(2025, 6, 15), 3, 150.00m),
            new(new DateOnly(2025, 6, 16), 5, 300.00m)
        };

        var summary = new DashboardSummary(10, 8, 3, 450.00m, stats);

        Assert.Equal(2, summary.DailyStats.Count);
        Assert.Equal(150.00m, summary.DailyStats[0].Revenue);
    }
}
