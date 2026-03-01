namespace Api.Features.Reports;

public interface IReportManager
{
    Task<DashboardSummary> GetDashboardSummaryAsync(DateOnly? from, DateOnly? to, CancellationToken ct = default);
}
