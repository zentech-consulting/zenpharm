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

        return app;
    }
}
