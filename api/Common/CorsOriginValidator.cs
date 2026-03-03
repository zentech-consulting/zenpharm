namespace Api.Common;

internal static class CorsOriginValidator
{
    /// <summary>
    /// Checks whether the given origin is allowed based on explicit origins and wildcard domain suffixes.
    /// </summary>
    internal static bool IsOriginAllowed(string origin, string[] allowedOrigins, string[] allowedDomains)
    {
        if (allowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
            return true;

        if (allowedDomains.Length == 0)
            return false;

        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
            return false;

        foreach (var domain in allowedDomains)
        {
            if (uri.Host.EndsWith($".{domain}", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
