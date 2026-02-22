using Microsoft.AspNetCore.Http;

namespace Api.Features.Clients;

public static class ClientEndpoints
{
    public static IEndpointRouteBuilder MapClientEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/clients")
            .WithTags("Clients")
            .RequireAuthorization();

        g.MapPost("", async Task<IResult> (CreateClientRequest req, IClientManager mgr, CancellationToken ct) =>
        {
            var client = await mgr.CreateAsync(req, ct);
            return Results.Created($"/api/clients/{client.Id}", client);
        })
        .WithOpenApi(op => { op.Summary = "Create a new client"; return op; });

        g.MapGet("{id:guid}", async Task<IResult> (Guid id, IClientManager mgr, CancellationToken ct) =>
        {
            var client = await mgr.GetByIdAsync(id, ct);
            return client is not null ? Results.Ok(client) : Results.NotFound();
        })
        .WithOpenApi(op => { op.Summary = "Get a client by ID"; return op; });

        g.MapGet("", async Task<IResult> (int? page, int? pageSize, string? search, IClientManager mgr, CancellationToken ct) =>
        {
            var result = await mgr.ListAsync(page ?? 1, pageSize ?? 20, search, ct);
            return Results.Ok(result);
        })
        .WithOpenApi(op => { op.Summary = "List clients with pagination and search"; return op; });

        g.MapPut("{id:guid}", async Task<IResult> (Guid id, UpdateClientRequest req, IClientManager mgr, CancellationToken ct) =>
        {
            var client = await mgr.UpdateAsync(id, req, ct);
            return client is not null ? Results.Ok(client) : Results.NotFound();
        })
        .WithOpenApi(op => { op.Summary = "Update a client"; return op; });

        g.MapDelete("{id:guid}", async Task<IResult> (Guid id, IClientManager mgr, CancellationToken ct) =>
        {
            var deleted = await mgr.DeleteAsync(id, ct);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .WithOpenApi(op => { op.Summary = "Delete a client"; return op; });

        return app;
    }
}
