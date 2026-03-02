using Microsoft.AspNetCore.Http;

namespace Api.Features.Reports;

public static class ReportEndpoints
{
    public static IEndpointRouteBuilder MapReportEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/reports")
            .WithTags("Reports")
            .RequireAuthorization();

        g.MapGet("dashboard", async Task<IResult> (DateOnly? from, DateOnly? to, IReportManager mgr, CancellationToken ct) =>
        {
            var summary = await mgr.GetDashboardSummaryAsync(from, to, ct);
            return Results.Ok(summary);
        })
        .WithOpenApi(op => { op.Summary = "Get dashboard summary statistics"; return op; });

        g.MapGet("top-selling-products", async Task<IResult> (DateOnly? from, DateOnly? to, int? limit, IReportManager mgr, CancellationToken ct) =>
        {
            var report = await mgr.GetTopSellingProductsAsync(from, to, Math.Clamp(limit ?? 10, 1, 50), ct);
            return Results.Ok(report);
        })
        .WithOpenApi(op => { op.Summary = "Top selling products by stock-out quantity"; return op; });

        g.MapGet("revenue-by-category", async Task<IResult> (DateOnly? from, DateOnly? to, IReportManager mgr, CancellationToken ct) =>
        {
            var report = await mgr.GetRevenueByCategoryAsync(from, to, ct);
            return Results.Ok(report);
        })
        .WithOpenApi(op => { op.Summary = "Revenue breakdown by service category"; return op; });

        g.MapGet("expiry-waste", async Task<IResult> (DateOnly? from, DateOnly? to, IReportManager mgr, CancellationToken ct) =>
        {
            var report = await mgr.GetExpiryWasteAsync(from, to, ct);
            return Results.Ok(report);
        })
        .WithOpenApi(op => { op.Summary = "Expiry waste analysis — expired stock value"; return op; });

        g.MapGet("employee-utilisation", async Task<IResult> (DateOnly? from, DateOnly? to, IReportManager mgr, CancellationToken ct) =>
        {
            var report = await mgr.GetEmployeeUtilisationAsync(from, to, ct);
            return Results.Ok(report);
        })
        .WithOpenApi(op => { op.Summary = "Employee utilisation — bookings per employee"; return op; });

        return app;
    }
}
