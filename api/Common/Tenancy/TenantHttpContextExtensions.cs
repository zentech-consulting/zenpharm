namespace Api.Common.Tenancy;

public static class TenantHttpContextExtensions
{
    /// <summary>
    /// Gets the TenantContext from the current request, or null if not resolved.
    /// </summary>
    public static TenantContext? GetTenantContext(this HttpContext context)
        => context.Items.TryGetValue("TenantContext", out var value) ? value as TenantContext : null;

    /// <summary>
    /// Gets the TenantContext from the current request, throwing if not available.
    /// Use in endpoints that require a tenant to be resolved.
    /// </summary>
    public static TenantContext RequireTenantContext(this HttpContext context)
        => context.GetTenantContext()
           ?? throw new InvalidOperationException(
               "TenantContext is required but was not resolved. Ensure TenantMiddleware is registered.");
}
