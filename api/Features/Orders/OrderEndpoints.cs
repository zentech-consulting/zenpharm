namespace Api.Features.Orders;

public static class OrderEndpoints
{
    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/orders")
            .WithTags("Orders")
            .RequireAuthorization();

        g.MapGet("", async Task<IResult> (
            int? page, int? pageSize, string? status, string? search,
            IOrderManager mgr, CancellationToken ct) =>
        {
            var result = await mgr.ListAsync(
                Math.Max(1, page ?? 1),
                Math.Clamp(pageSize ?? 20, 1, 100),
                status, search, ct);
            return Results.Ok(result);
        })
        .WithOpenApi(op => { op.Summary = "List orders (admin)"; return op; });

        g.MapGet("{id:guid}", async Task<IResult> (Guid id, IOrderManager mgr, CancellationToken ct) =>
        {
            var order = await mgr.GetByIdAsync(id, ct);
            return order is not null ? Results.Ok(order) : Results.NotFound();
        })
        .WithOpenApi(op => { op.Summary = "Get order by ID (admin)"; return op; });

        g.MapPost("{id:guid}/mark-ready", async Task<IResult> (Guid id, IOrderManager mgr, CancellationToken ct) =>
        {
            var order = await mgr.MarkAsReadyAsync(id, ct);
            return order is not null ? Results.Ok(order) : Results.NotFound();
        })
        .WithOpenApi(op => { op.Summary = "Mark order as ready (sends SMS)"; return op; });

        g.MapPost("{id:guid}/mark-collected", async Task<IResult> (Guid id, IOrderManager mgr, CancellationToken ct) =>
        {
            var order = await mgr.MarkAsCollectedAsync(id, ct);
            return order is not null ? Results.Ok(order) : Results.NotFound();
        })
        .WithOpenApi(op => { op.Summary = "Mark order as collected"; return op; });

        g.MapPost("{id:guid}/cancel", async Task<IResult> (
            Guid id, CancelOrderRequest req, IOrderManager mgr, CancellationToken ct) =>
        {
            try
            {
                var order = await mgr.CancelOrderAsync(id, req.Reason, ct);
                return order is not null ? Results.Ok(order) : Results.NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithOpenApi(op => { op.Summary = "Cancel an order with reason"; return op; });

        return app;
    }
}
