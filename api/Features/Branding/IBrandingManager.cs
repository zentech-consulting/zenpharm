namespace Api.Features.Branding;

internal interface IBrandingManager
{
    Task<BrandingResponse?> GetBrandingAsync(Guid tenantId, CancellationToken ct = default);
}
