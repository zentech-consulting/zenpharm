using Api.Features.Orders;

namespace Api.Features.Shop;

public static class ShopEndpoints
{
    public static IEndpointRouteBuilder MapShopEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/shop")
            .WithTags("Shop")
            .AllowAnonymous();

        // --- Products ---

        g.MapGet("products", async Task<IResult> (
            string? category, string? search, bool? featured,
            int? page, int? pageSize,
            IShopManager mgr, CancellationToken ct) =>
        {
            var result = await mgr.ListProductsAsync(
                category, search, featured,
                Math.Max(1, page ?? 1),
                Math.Clamp(pageSize ?? 20, 1, 50),
                ct);
            return Results.Ok(result);
        })
        .WithOpenApi(op => { op.Summary = "Browse products (public, excludes S4)"; return op; });

        g.MapGet("products/{id:guid}", async Task<IResult> (Guid id, IShopManager mgr, CancellationToken ct) =>
        {
            var product = await mgr.GetProductAsync(id, ct);
            return product is not null ? Results.Ok(product) : Results.NotFound();
        })
        .WithOpenApi(op => { op.Summary = "Get product detail (public, excludes S4)"; return op; });

        g.MapGet("categories", async Task<IResult> (IShopManager mgr, CancellationToken ct) =>
        {
            var categories = await mgr.ListCategoriesAsync(ct);
            return Results.Ok(categories);
        })
        .WithOpenApi(op => { op.Summary = "List product categories (public)"; return op; });

        // --- Orders (public) ---

        g.MapPost("orders", async Task<IResult> (
            CreateGuestOrderRequest req, IOrderManager mgr, CancellationToken ct) =>
        {
            try
            {
                var order = await mgr.CreateGuestOrderAsync(req, ct);
                return Results.Created($"/api/shop/orders/{order.OrderNumber}", order);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .RequireRateLimiting("shop-orders")
        .WithOpenApi(op => { op.Summary = "Place a guest click-and-collect order"; return op; });

        g.MapGet("orders/{orderNumber}", async Task<IResult> (
            string orderNumber, IOrderManager mgr, CancellationToken ct) =>
        {
            var order = await mgr.GetByOrderNumberAsync(orderNumber, ct);
            return order is not null ? Results.Ok(order) : Results.NotFound();
        })
        .WithOpenApi(op => { op.Summary = "Track an order by order number (public)"; return op; });

        return app;
    }
}
