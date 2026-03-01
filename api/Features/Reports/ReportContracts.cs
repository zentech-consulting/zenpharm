namespace Api.Features.Reports;

public sealed record DashboardSummary(
    int TotalClients,
    int TotalBookings,
    int TotalEmployees,
    decimal Revenue,
    IReadOnlyList<DailyStat> DailyStats);

public sealed record DailyStat(
    DateOnly Date,
    int BookingCount,
    decimal Revenue);
