namespace Api.Features.Platform;

public static class TenantManagementEndpoints
{
    public static void MapPlatformEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/platform")
            .WithTags("Platform")
            .RequireAuthorization();

        group.MapGet("/tenants", () =>
        {
            throw new NotImplementedException("Platform tenant listing not yet implemented — see Phase 1, Subtask 3");
        })
        .WithOpenApi(op => { op.Summary = "List all tenants (platform admin)"; return op; });

        group.MapPost("/tenants", () =>
        {
            throw new NotImplementedException("Platform tenant provisioning not yet implemented — see Phase 1, Subtask 3");
        })
        .WithOpenApi(op => { op.Summary = "Provision a new tenant (platform admin)"; return op; });
    }
}
