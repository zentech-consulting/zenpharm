namespace Api.Common.Tenancy;

/// <summary>
/// Validates that the tenant_id claim in the JWT matches the resolved TenantContext.
/// Runs after UseAuthentication to prevent cross-tenant access via stolen tokens.
/// Anonymous requests and requests without tenant_id claim are allowed through
/// for backwards compatibility.
/// </summary>
internal sealed class TenantClaimValidationMiddleware(
    RequestDelegate next,
    ILogger<TenantClaimValidationMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        // Skip if user is not authenticated
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await next(context);
            return;
        }

        // Skip if no tenant context (e.g. platform endpoints, health checks)
        if (context.Items["TenantContext"] is not TenantContext tenantContext)
        {
            await next(context);
            return;
        }

        // Skip if the JWT has no tenant_id claim (backwards compat with pre-Phase4 tokens)
        var tenantIdClaim = context.User.FindFirst("tenant_id")?.Value;
        if (string.IsNullOrEmpty(tenantIdClaim))
        {
            await next(context);
            return;
        }

        // Validate: JWT tenant_id must match resolved tenant
        if (!Guid.TryParse(tenantIdClaim, out var claimTenantId) || claimTenantId != tenantContext.TenantId)
        {
            logger.LogWarning(
                "Tenant claim mismatch. JWT tenant_id={ClaimTenantId} but resolved tenant={ResolvedTenantId}",
                tenantIdClaim, tenantContext.TenantId);

            context.Response.StatusCode = 403;
            await context.Response.WriteAsJsonAsync(new { error = "Token not valid for this tenant." });
            return;
        }

        await next(context);
    }
}

public static class TenantClaimValidationExtensions
{
    public static IApplicationBuilder UseTenantClaimValidation(this IApplicationBuilder app)
        => app.UseMiddleware<TenantClaimValidationMiddleware>();
}
