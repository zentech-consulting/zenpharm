using Microsoft.AspNetCore.Http;

namespace Api.Features.Services;

public static class ServiceEndpoints
{
    public static IEndpointRouteBuilder MapServiceEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/services")
            .WithTags("Services");

        g.MapPost("", async Task<IResult> (CreateServiceRequest req, IServiceManager mgr, CancellationToken ct) =>
        {
            var service = await mgr.CreateAsync(req, ct);
            return Results.Created($"/api/services/{service.Id}", service);
        })
        .RequireAuthorization()
        .WithOpenApi(op => { op.Summary = "Create a new service item"; return op; });

        g.MapGet("{id:guid}", async Task<IResult> (Guid id, IServiceManager mgr, CancellationToken ct) =>
        {
            var service = await mgr.GetByIdAsync(id, ct);
            return service is not null ? Results.Ok(service) : Results.NotFound();
        })
        .AllowAnonymous()
        .WithOpenApi(op => { op.Summary = "Get a service by ID"; return op; });

        g.MapGet("", async Task<IResult> (int? page, int? pageSize, string? category, IServiceManager mgr, CancellationToken ct) =>
        {
            var result = await mgr.ListAsync(Math.Max(1, page ?? 1), Math.Clamp(pageSize ?? 20, 1, 100), category, ct);
            return Results.Ok(result);
        })
        .AllowAnonymous()
        .WithOpenApi(op => { op.Summary = "List services with pagination"; return op; });

        g.MapPut("{id:guid}", async Task<IResult> (Guid id, UpdateServiceRequest req, IServiceManager mgr, CancellationToken ct) =>
        {
            var service = await mgr.UpdateAsync(id, req, ct);
            return service is not null ? Results.Ok(service) : Results.NotFound();
        })
        .RequireAuthorization()
        .WithOpenApi(op => { op.Summary = "Update a service item"; return op; });

        g.MapDelete("{id:guid}", async Task<IResult> (Guid id, IServiceManager mgr, CancellationToken ct) =>
        {
            var deleted = await mgr.DeleteAsync(id, ct);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .RequireAuthorization()
        .WithOpenApi(op => { op.Summary = "Delete a service item"; return op; });

        return app;
    }
}
