using System.Text.RegularExpressions;

namespace Api.Common.Tenancy;

/// <summary>
/// Extracts subdomain from the Host header, resolves the tenant,
/// and injects TenantContext into HttpContext.Items for the request scope.
/// </summary>
internal sealed partial class TenantMiddleware(
    RequestDelegate next,
    ITenantResolver resolver,
    IConfiguration configuration,
    ILogger<TenantMiddleware> logger)
{
    private static readonly HashSet<string> ReservedSubdomains =
        new(StringComparer.OrdinalIgnoreCase) { "www", "api", "admin", "mail", "ftp" };

    private static readonly string[] BypassPrefixes =
        ["/health", "/swagger", "/api/platform"];

    [GeneratedRegex(@"^[a-z0-9]([a-z0-9\-]{0,61}[a-z0-9])?$", RegexOptions.IgnoreCase)]
    private static partial Regex SubdomainPattern();

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";

        // Bypass paths skip tenant resolution entirely
        if (BypassPrefixes.Any(prefix => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
        {
            await next(context);
            return;
        }

        var host = context.Request.Host.Host;
        var subdomain = ExtractSubdomain(host, configuration);

        // No subdomain (naked domain or reserved) → pass through without tenant
        if (subdomain is null)
        {
            await next(context);
            return;
        }

        // Validate subdomain format (DNS label: alphanumeric + hyphens, max 63 chars)
        if (!IsValidSubdomain(subdomain))
        {
            logger.LogWarning("Invalid subdomain format: {Subdomain}", subdomain);
            context.Response.StatusCode = 400;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid subdomain format." });
            return;
        }

        var tenant = await resolver.ResolveAsync(subdomain, context.RequestAborted);

        if (tenant is null)
        {
            logger.LogWarning("Unknown tenant subdomain: {Subdomain} (host: {Host})", subdomain, host);
            context.Response.StatusCode = 404;
            await context.Response.WriteAsJsonAsync(new { error = "Tenant not found." });
            return;
        }

        if (!tenant.IsActive)
        {
            logger.LogWarning("Inactive tenant: {Subdomain} (status: {Status})", subdomain, tenant.Status);
            context.Response.StatusCode = 403;
            await context.Response.WriteAsJsonAsync(new { error = "Tenant account is not active." });
            return;
        }

        context.Items["TenantContext"] = tenant;
        await next(context);
    }

    /// <summary>
    /// Validates subdomain format per DNS label rules (RFC 1123):
    /// alphanumeric + hyphens, 1-63 chars, cannot start/end with hyphen.
    /// </summary>
    internal static bool IsValidSubdomain(string subdomain)
        => !string.IsNullOrWhiteSpace(subdomain)
           && subdomain.Length <= 63
           && SubdomainPattern().IsMatch(subdomain);

    /// <summary>
    /// Extracts the subdomain from a host string.
    /// Handles compound TLDs like .com.au, .co.uk, .co.nz.
    /// Returns null for localhost (uses config fallback), reserved subdomains, or naked domains.
    /// </summary>
    internal static string? ExtractSubdomain(string host, IConfiguration configuration)
    {
        // IP addresses → dev fallback
        if (System.Net.IPAddress.TryParse(host, out _))
            return NullIfEmpty(configuration["Tenancy:DevTenantSubdomain"]);

        // localhost → dev fallback
        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
            return NullIfEmpty(configuration["Tenancy:DevTenantSubdomain"]);

        var parts = host.Split('.');

        // Compound TLDs: .com.au, .co.uk, .co.nz, .org.au, etc.
        var tldSegments = GetTldSegmentCount(parts);
        var domainParts = parts.Length - tldSegments;

        // Naked domain (e.g. zenpharm.com.au → 3 parts, 2 TLD segments, 1 domain part)
        if (domainParts <= 1)
            return null;

        // Subdomain is everything before the registered domain
        // e.g. smithpharmacy.zenpharm.com.au → subdomain = "smithpharmacy"
        // e.g. demo.example.com → subdomain = "demo"
        var subdomain = parts[0];

        if (ReservedSubdomains.Contains(subdomain))
            return null;

        return subdomain;
    }

    /// <summary>Treats empty/whitespace config values as null to avoid invalid subdomain errors.</summary>
    private static string? NullIfEmpty(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value;

    /// <summary>
    /// Returns the number of TLD segments for common compound TLDs.
    /// </summary>
    private static int GetTldSegmentCount(string[] hostParts)
    {
        if (hostParts.Length < 2)
            return 1;

        var last = hostParts[^1];
        var secondLast = hostParts[^2];

        // Two-segment TLDs: .com.au, .co.uk, .co.nz, .org.au, .net.au, .co.in
        if (last is "au" or "uk" or "nz" or "in" or "za" or "jp" or "br" or "kr")
        {
            if (secondLast is "com" or "co" or "org" or "net" or "edu" or "gov")
                return 2;
        }

        return 1;
    }
}
