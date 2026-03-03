using Api.Features.Branding;
using Xunit;

namespace Api.Tests.Branding;

public class BrandingContractTests
{
    [Fact]
    public void BrandingResponse_RecordEquality()
    {
        var a = new BrandingResponse(
            DisplayName: "Smith Pharmacy",
            ShortName: "SP",
            LogoUrl: null,
            FaviconUrl: null,
            PrimaryColour: "#1a1a2e",
            SecondaryColour: "#16213e",
            AccentColour: "#0f3460",
            HighlightColour: "#e94560",
            Tagline: "Your local pharmacy",
            ContactEmail: "info@smith.com",
            ContactPhone: "0400000000",
            Abn: "12345678901",
            AddressLine1: "123 Main St",
            AddressLine2: null,
            Suburb: "Sydney",
            State: "NSW",
            Postcode: "2000",
            BusinessHoursJson: null,
            Plan: "Basic");

        var b = new BrandingResponse(
            DisplayName: "Smith Pharmacy",
            ShortName: "SP",
            LogoUrl: null,
            FaviconUrl: null,
            PrimaryColour: "#1a1a2e",
            SecondaryColour: "#16213e",
            AccentColour: "#0f3460",
            HighlightColour: "#e94560",
            Tagline: "Your local pharmacy",
            ContactEmail: "info@smith.com",
            ContactPhone: "0400000000",
            Abn: "12345678901",
            AddressLine1: "123 Main St",
            AddressLine2: null,
            Suburb: "Sydney",
            State: "NSW",
            Postcode: "2000",
            BusinessHoursJson: null,
            Plan: "Basic");

        Assert.Equal(a, b);
    }

    [Fact]
    public void BrandingResponse_NullableFieldsDefaultCorrectly()
    {
        var branding = new BrandingResponse(
            DisplayName: "Test",
            ShortName: null,
            LogoUrl: null,
            FaviconUrl: null,
            PrimaryColour: "#000000",
            SecondaryColour: null,
            AccentColour: null,
            HighlightColour: null,
            Tagline: null,
            ContactEmail: null,
            ContactPhone: null,
            Abn: null,
            AddressLine1: null,
            AddressLine2: null,
            Suburb: null,
            State: null,
            Postcode: null,
            BusinessHoursJson: null,
            Plan: "Free");

        Assert.Equal("Test", branding.DisplayName);
        Assert.Equal("#000000", branding.PrimaryColour);
        Assert.Equal("Free", branding.Plan);
        Assert.Null(branding.ShortName);
        Assert.Null(branding.HighlightColour);
        Assert.Null(branding.Tagline);
    }

    [Fact]
    public void BrandingResponse_Serialisation_RoundTrip()
    {
        var branding = new BrandingResponse(
            DisplayName: "Acme Pharmacy",
            ShortName: "AP",
            LogoUrl: "https://example.com/logo.png",
            FaviconUrl: "https://example.com/favicon.ico",
            PrimaryColour: "#1a1a2e",
            SecondaryColour: "#16213e",
            AccentColour: "#0f3460",
            HighlightColour: "#e94560",
            Tagline: "Health care you can trust",
            ContactEmail: "info@acme.com",
            ContactPhone: "0412345678",
            Abn: "98765432101",
            AddressLine1: "456 High St",
            AddressLine2: "Suite 2",
            Suburb: "Melbourne",
            State: "VIC",
            Postcode: "3000",
            BusinessHoursJson: """{"mon":"8:30-17:30"}""",
            Plan: "Premium");

        var json = System.Text.Json.JsonSerializer.Serialize(branding);
        var deserialised = System.Text.Json.JsonSerializer.Deserialize<BrandingResponse>(json);

        Assert.Equal(branding, deserialised);
    }

    [Fact]
    public void IBrandingManager_InterfaceExists()
    {
        // Verify the interface is accessible (internal via InternalsVisibleTo)
        var type = typeof(IBrandingManager);
        Assert.True(type.IsInterface);
        Assert.Contains("GetBrandingAsync", type.GetMethods().Select(m => m.Name));
    }
}
