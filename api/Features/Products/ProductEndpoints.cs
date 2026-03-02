using Microsoft.AspNetCore.Http;

namespace Api.Features.Products;

public static class ProductEndpoints
{
    public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/products")
            .WithTags("Products")
            .RequireAuthorization();

        g.MapGet("{id:guid}", async Task<IResult> (Guid id, IProductManager mgr, CancellationToken ct) =>
        {
            var product = await mgr.GetByIdAsync(id, ct);
            return product is not null ? Results.Ok(product) : Results.NotFound();
        })
        .WithOpenApi(op => { op.Summary = "Get a tenant product by ID"; return op; });

        g.MapGet("", async Task<IResult> (
            int? page, int? pageSize, string? search, bool? lowStockOnly, bool? expiringOnly,
            IProductManager mgr, CancellationToken ct) =>
        {
            var result = await mgr.ListAsync(
                Math.Max(1, page ?? 1),
                Math.Clamp(pageSize ?? 20, 1, 100),
                search, lowStockOnly ?? false, expiringOnly ?? false, ct);
            return Results.Ok(result);
        })
        .WithOpenApi(op => { op.Summary = "List tenant products with filters"; return op; });

        g.MapPost("import", async Task<IResult> (ImportProductsRequest req, IProductManager mgr, CancellationToken ct) =>
        {
            var imported = await mgr.ImportFromCatalogueAsync(req.MasterProductIds, ct);
            return Results.Ok(imported);
        })
        .WithOpenApi(op => { op.Summary = "Import products from the shared catalogue"; return op; });

        g.MapPut("{id:guid}", async Task<IResult> (Guid id, UpdateProductRequest req, IProductManager mgr, CancellationToken ct) =>
        {
            var product = await mgr.UpdateAsync(id, req, ct);
            return product is not null ? Results.Ok(product) : Results.NotFound();
        })
        .WithOpenApi(op => { op.Summary = "Update a tenant product"; return op; });

        g.MapDelete("{id:guid}", async Task<IResult> (Guid id, IProductManager mgr, CancellationToken ct) =>
        {
            var deleted = await mgr.DeleteAsync(id, ct);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .WithOpenApi(op => { op.Summary = "Delete a tenant product"; return op; });

        g.MapPost("{id:guid}/stock-movements",
            async Task<IResult> (Guid id, RecordStockMovementRequest req, IProductManager mgr, CancellationToken ct) =>
        {
            try
            {
                var movement = await mgr.RecordStockMovementAsync(id, req, ct);
                return Results.Created($"/api/products/{id}/stock-movements", movement);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithOpenApi(op => { op.Summary = "Record a stock movement for a product"; return op; });

        g.MapGet("{id:guid}/stock-movements",
            async Task<IResult> (Guid id, int? page, int? pageSize, IProductManager mgr, CancellationToken ct) =>
        {
            var result = await mgr.ListStockMovementsAsync(id, Math.Max(1, page ?? 1), Math.Clamp(pageSize ?? 20, 1, 100), ct);
            return Results.Ok(result);
        })
        .WithOpenApi(op => { op.Summary = "List stock movements for a product"; return op; });

        g.MapGet("low-stock", async Task<IResult> (IProductManager mgr, CancellationToken ct) =>
        {
            var summary = await mgr.GetLowStockSummaryAsync(ct);
            return Results.Ok(summary);
        })
        .WithOpenApi(op => { op.Summary = "Get low stock summary"; return op; });

        g.MapGet("expiry-alerts", async Task<IResult> (int? daysAhead, IProductManager mgr, CancellationToken ct) =>
        {
            var alerts = await mgr.GetExpiryAlertsAsync(daysAhead ?? 30, ct);
            return Results.Ok(alerts);
        })
        .WithOpenApi(op => { op.Summary = "Get expiry alerts for products nearing expiration"; return op; });

        return app;
    }
}
