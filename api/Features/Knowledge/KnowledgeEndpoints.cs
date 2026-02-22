using Microsoft.AspNetCore.Http;

namespace Api.Features.Knowledge;

public static class KnowledgeEndpoints
{
    public static IEndpointRouteBuilder MapKnowledgeEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/knowledge")
            .WithTags("Knowledge Base");

        g.MapPost("", async Task<IResult> (CreateKnowledgeEntryRequest req, IKnowledgeManager mgr, CancellationToken ct) =>
        {
            var entry = await mgr.CreateAsync(req, ct);
            return Results.Created($"/api/knowledge/{entry.Id}", entry);
        })
        .RequireAuthorization()
        .WithOpenApi(op => { op.Summary = "Create a knowledge base entry"; return op; });

        g.MapGet("{id:guid}", async Task<IResult> (Guid id, IKnowledgeManager mgr, CancellationToken ct) =>
        {
            var entry = await mgr.GetByIdAsync(id, ct);
            return entry is not null ? Results.Ok(entry) : Results.NotFound();
        })
        .RequireAuthorization()
        .WithOpenApi(op => { op.Summary = "Get a knowledge entry by ID"; return op; });

        g.MapGet("", async Task<IResult> (int? page, int? pageSize, string? category, IKnowledgeManager mgr, CancellationToken ct) =>
        {
            var result = await mgr.ListAsync(Math.Max(1, page ?? 1), Math.Clamp(pageSize ?? 20, 1, 100), category, ct);
            return Results.Ok(result);
        })
        .RequireAuthorization()
        .WithOpenApi(op => { op.Summary = "List knowledge entries"; return op; });

        g.MapPut("{id:guid}", async Task<IResult> (Guid id, UpdateKnowledgeEntryRequest req, IKnowledgeManager mgr, CancellationToken ct) =>
        {
            var entry = await mgr.UpdateAsync(id, req, ct);
            return entry is not null ? Results.Ok(entry) : Results.NotFound();
        })
        .RequireAuthorization()
        .WithOpenApi(op => { op.Summary = "Update a knowledge entry"; return op; });

        g.MapDelete("{id:guid}", async Task<IResult> (Guid id, IKnowledgeManager mgr, CancellationToken ct) =>
        {
            var deleted = await mgr.DeleteAsync(id, ct);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .RequireAuthorization()
        .WithOpenApi(op => { op.Summary = "Delete a knowledge entry"; return op; });

        g.MapPost("search", async Task<IResult> (KnowledgeSearchRequest req, IKnowledgeManager mgr, CancellationToken ct) =>
        {
            var results = await mgr.SearchAsync(req, ct);
            return Results.Ok(results);
        })
        .AllowAnonymous()
        .WithOpenApi(op => { op.Summary = "Search the knowledge base"; return op; });

        return app;
    }
}
