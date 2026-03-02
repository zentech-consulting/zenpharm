namespace Api.Features.Reports;

public interface IReportManager
{
    Task<DashboardSummary> GetDashboardSummaryAsync(DateOnly? from, DateOnly? to, CancellationToken ct = default);
    Task<TopSellingProductsReport> GetTopSellingProductsAsync(DateOnly? from, DateOnly? to, int limit, CancellationToken ct = default);
    Task<RevenueByCategoryReport> GetRevenueByCategoryAsync(DateOnly? from, DateOnly? to, CancellationToken ct = default);
    Task<ExpiryWasteReport> GetExpiryWasteAsync(DateOnly? from, DateOnly? to, CancellationToken ct = default);
    Task<EmployeeUtilisationReport> GetEmployeeUtilisationAsync(DateOnly? from, DateOnly? to, CancellationToken ct = default);
}
