using Microsoft.AspNetCore.Http;

namespace Api.Features.MasterProducts;

public static class MasterProductEndpoints
{
    public static IEndpointRouteBuilder MapMasterProductEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/master-products")
            .WithTags("MasterProducts");

        g.MapPost("", async Task<IResult> (CreateMasterProductRequest req, IMasterProductManager mgr, CancellationToken ct) =>
        {
            var product = await mgr.CreateAsync(req, ct);
            return Results.Created($"/api/master-products/{product.Id}", product);
        })
        .RequireAuthorization()
        .WithOpenApi(op => { op.Summary = "Create a master product in the shared catalogue"; return op; });

        g.MapGet("{id:guid}", async Task<IResult> (Guid id, IMasterProductManager mgr, CancellationToken ct) =>
        {
            var product = await mgr.GetByIdAsync(id, ct);
            return product is not null ? Results.Ok(product) : Results.NotFound();
        })
        .AllowAnonymous()
        .WithOpenApi(op => { op.Summary = "Get a master product by ID"; return op; });

        g.MapGet("", async Task<IResult> (
            int? page, int? pageSize, string? category, string? search, string? scheduleClass,
            IMasterProductManager mgr, CancellationToken ct) =>
        {
            var result = await mgr.ListAsync(
                Math.Max(1, page ?? 1),
                Math.Clamp(pageSize ?? 20, 1, 100),
                category, search, scheduleClass, ct);
            return Results.Ok(result);
        })
        .AllowAnonymous()
        .WithOpenApi(op => { op.Summary = "List master products with filters"; return op; });

        g.MapPut("{id:guid}", async Task<IResult> (Guid id, UpdateMasterProductRequest req, IMasterProductManager mgr, CancellationToken ct) =>
        {
            var product = await mgr.UpdateAsync(id, req, ct);
            return product is not null ? Results.Ok(product) : Results.NotFound();
        })
        .RequireAuthorization()
        .WithOpenApi(op => { op.Summary = "Update a master product"; return op; });

        return app;
    }
}
