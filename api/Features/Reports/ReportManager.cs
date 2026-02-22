namespace Api.Features.Reports;

internal sealed class ReportManager(
    ILogger<ReportManager> logger) : IReportManager
{
    public Task<DashboardSummary> GetDashboardSummaryAsync(DateOnly? from, DateOnly? to, CancellationToken ct = default)
    {
        throw new NotImplementedException("Report module not yet implemented — see Phase 1, Subtask 13");
    }
}
