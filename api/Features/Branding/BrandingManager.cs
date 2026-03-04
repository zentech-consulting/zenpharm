using Api.Common;
using Dapper;

namespace Api.Features.Branding;

internal sealed class BrandingManager(
    ICatalogDb catalogDb) : IBrandingManager
{
    public async Task<BrandingResponse?> GetBrandingAsync(Guid tenantId, CancellationToken ct = default)
    {
        using var conn = await catalogDb.CreateAsync();

        const string sql = """
            SELECT
                t.DisplayName, t.ShortName, t.LogoUrl, t.FaviconUrl,
                t.PrimaryColour, t.SecondaryColour, t.AccentColour, t.HighlightColour,
                t.Tagline, t.ContactEmail, t.ContactPhone,
                t.Abn, t.AddressLine1, t.AddressLine2, t.Suburb, t.State, t.Postcode,
                t.BusinessHoursJson,
                COALESCE(s.PlanName, 'Free') AS [Plan]
            FROM dbo.Tenants t
            LEFT JOIN dbo.Subscriptions s ON s.TenantId = t.Id AND s.Status = 'Active'
            WHERE t.Id = @TenantId AND t.Status = 'Active';
            """;

        return await conn.QueryFirstOrDefaultAsync<BrandingResponse>(
            new CommandDefinition(sql, new { TenantId = tenantId }, cancellationToken: ct));
    }
}
