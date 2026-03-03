using Api.Common.Tenancy;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Api.Tests.Tenancy;

public class SubdomainExtractionTests
{
    private static IConfiguration CreateConfig(string? devSubdomain = null)
    {
        var dict = new Dictionary<string, string?>();
        if (devSubdomain is not null)
            dict["Tenancy:DevTenantSubdomain"] = devSubdomain;

        return new ConfigurationBuilder()
            .AddInMemoryCollection(dict)
            .Build();
    }

    [Fact]
    public void CompoundTld_ExtractsSubdomain()
    {
        // smithpharmacy.zenpharm.com.au → "smithpharmacy"
        var result = TenantMiddleware.ExtractSubdomain("smithpharmacy.zenpharm.com.au", CreateConfig());
        Assert.Equal("smithpharmacy", result);
    }

    [Fact]
    public void Localhost_ReturnsDevFallback()
    {
        var result = TenantMiddleware.ExtractSubdomain("localhost", CreateConfig("dev"));
        Assert.Equal("dev", result);
    }

    [Fact]
    public void Localhost_NoConfig_ReturnsNull()
    {
        var result = TenantMiddleware.ExtractSubdomain("localhost", CreateConfig());
        Assert.Null(result);
    }

    [Fact]
    public void LocalhostWithPort_HandledByHostHeader()
    {
        // The Host header splits off the port before reaching ExtractSubdomain,
        // but if the full host:port is passed, localhost is still detected.
        // In ASP.NET, Request.Host.Host strips the port, so we test "localhost" directly.
        var result = TenantMiddleware.ExtractSubdomain("localhost", CreateConfig("dev"));
        Assert.Equal("dev", result);
    }

    [Fact]
    public void WwwReserved_ReturnsNull()
    {
        var result = TenantMiddleware.ExtractSubdomain("www.zenpharm.com.au", CreateConfig());
        Assert.Null(result);
    }

    [Fact]
    public void AdminReserved_ReturnsNull()
    {
        var result = TenantMiddleware.ExtractSubdomain("admin.zenpharm.com.au", CreateConfig());
        Assert.Null(result);
    }

    [Fact]
    public void NakedDomain_ReturnsNull()
    {
        // zenpharm.com.au → naked domain, no subdomain
        var result = TenantMiddleware.ExtractSubdomain("zenpharm.com.au", CreateConfig());
        Assert.Null(result);
    }

    [Fact]
    public void IpAddress_ReturnsDevFallback()
    {
        var result = TenantMiddleware.ExtractSubdomain("127.0.0.1", CreateConfig("dev"));
        Assert.Equal("dev", result);
    }

    [Fact]
    public void IpAddress_NoConfig_ReturnsNull()
    {
        var result = TenantMiddleware.ExtractSubdomain("127.0.0.1", CreateConfig());
        Assert.Null(result);
    }

    [Fact]
    public void Localhost_EmptyConfig_ReturnsNull()
    {
        // Empty string in config should be treated as null (no tenant)
        var result = TenantMiddleware.ExtractSubdomain("localhost", CreateConfig(""));
        Assert.Null(result);
    }

    [Fact]
    public void IpAddress_EmptyConfig_ReturnsNull()
    {
        var result = TenantMiddleware.ExtractSubdomain("127.0.0.1", CreateConfig(""));
        Assert.Null(result);
    }

    [Fact]
    public void SimpleTld_ExtractsSubdomain()
    {
        // demo.example.com → "demo"
        var result = TenantMiddleware.ExtractSubdomain("demo.example.com", CreateConfig());
        Assert.Equal("demo", result);
    }

    [Fact]
    public void CoUk_ExtractsSubdomain()
    {
        // shop.example.co.uk → "shop"
        var result = TenantMiddleware.ExtractSubdomain("shop.example.co.uk", CreateConfig());
        Assert.Equal("shop", result);
    }

    [Fact]
    public void NakedDomainSimpleTld_ReturnsNull()
    {
        // example.com → naked domain
        var result = TenantMiddleware.ExtractSubdomain("example.com", CreateConfig());
        Assert.Null(result);
    }

    [Fact]
    public void ApiReserved_ReturnsNull()
    {
        var result = TenantMiddleware.ExtractSubdomain("api.zenpharm.com.au", CreateConfig());
        Assert.Null(result);
    }

    // --- Subdomain validation tests ---

    [Theory]
    [InlineData("smithpharmacy", true)]
    [InlineData("demo", true)]
    [InlineData("my-pharmacy", true)]
    [InlineData("a", true)]
    [InlineData("123", true)]
    [InlineData("dev-01", true)]
    public void IsValidSubdomain_ValidInputs_ReturnsTrue(string subdomain, bool expected)
    {
        Assert.Equal(expected, TenantMiddleware.IsValidSubdomain(subdomain));
    }

    [Theory]
    [InlineData("", false)]
    [InlineData(" ", false)]
    [InlineData("-invalid", false)]
    [InlineData("invalid-", false)]
    [InlineData("has space", false)]
    [InlineData("has.dot", false)]
    [InlineData("has_underscore", false)]
    [InlineData("CAPS-ok", true)]  // case insensitive regex
    public void IsValidSubdomain_EdgeCases(string subdomain, bool expected)
    {
        Assert.Equal(expected, TenantMiddleware.IsValidSubdomain(subdomain));
    }

    [Fact]
    public void IsValidSubdomain_TooLong_ReturnsFalse()
    {
        var longSubdomain = new string('a', 64);  // DNS label max is 63
        Assert.False(TenantMiddleware.IsValidSubdomain(longSubdomain));
    }

    [Fact]
    public void IsValidSubdomain_MaxLength_ReturnsTrue()
    {
        var maxSubdomain = new string('a', 63);
        Assert.True(TenantMiddleware.IsValidSubdomain(maxSubdomain));
    }

    // --- X-Tenant-Subdomain header tests ---

    [Fact]
    public void TenantSubdomainHeader_ConstantName()
    {
        Assert.Equal("X-Tenant-Subdomain", TenantMiddleware.TenantSubdomainHeader);
    }
}
