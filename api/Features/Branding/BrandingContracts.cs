namespace Api.Features.Branding;

public sealed record BrandingResponse(
    string DisplayName,
    string? ShortName,
    string? LogoUrl,
    string? FaviconUrl,
    string PrimaryColour,
    string? SecondaryColour,
    string? AccentColour,
    string? HighlightColour,
    string? Tagline,
    string? ContactEmail,
    string? ContactPhone,
    string? Abn,
    string? AddressLine1,
    string? AddressLine2,
    string? Suburb,
    string? State,
    string? Postcode,
    string? BusinessHoursJson,
    string Plan);
