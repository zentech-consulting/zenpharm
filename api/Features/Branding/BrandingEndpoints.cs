using Api.Common.Tenancy;

namespace Api.Features.Branding;

public static class BrandingEndpoints
{
    public static IEndpointRouteBuilder MapBrandingEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/branding", async Task<IResult> (IBrandingManager mgr, HttpContext ctx, CancellationToken ct) =>
        {
            var tenantContext = ctx.Items["TenantContext"] as TenantContext;
            if (tenantContext is null)
                return Results.NotFound(new { error = "Tenant not resolved." });

            var branding = await mgr.GetBrandingAsync(tenantContext.TenantId, ct);
            return branding is not null ? Results.Ok(branding) : Results.NotFound(new { error = "Branding not found." });
        })
        .AllowAnonymous()
        .RequireRateLimiting("branding")
        .WithTags("Branding")
        .WithOpenApi(op => { op.Summary = "Get tenant branding configuration"; return op; });

        return app;
    }
}
