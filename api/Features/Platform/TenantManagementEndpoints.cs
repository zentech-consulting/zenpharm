using Api.Common;
using Dapper;

namespace Api.Features.Platform;

public static class TenantManagementEndpoints
{
    public static void MapPlatformEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/platform")
            .WithTags("Platform")
            .RequireAuthorization();

        group.MapGet("/tenants", async (ICatalogDb catalogDb, CancellationToken ct) =>
        {
            using var conn = await catalogDb.CreateAsync();
            var tenants = await conn.QueryAsync<dynamic>(
                new CommandDefinition(
                    "SELECT Id, Name, Subdomain, IsActive, CreatedAt FROM dbo.Tenants ORDER BY CreatedAt DESC",
                    cancellationToken: ct));
            return Results.Ok(tenants);
        })
        .WithOpenApi(op => { op.Summary = "List all tenants (platform admin)"; return op; });

        group.MapPost("/tenants", async (
            ProvisionRequest req,
            IProvisioningPipeline pipeline,
            CancellationToken ct) =>
        {
            var result = await pipeline.ProvisionAsync(req, ct);
            return result.Success
                ? Results.Created($"/api/platform/tenants/{result.TenantId}", result)
                : Results.BadRequest(result);
        })
        .WithOpenApi(op => { op.Summary = "Provision a new tenant (platform admin)"; return op; });
    }
}
