using Api.Common;
using Xunit;

namespace Api.Tests.Cors;

public class CorsOriginValidatorTests
{
    [Fact]
    public void ExplicitOrigin_Allowed()
    {
        var origins = new[] { "http://localhost:51000", "http://localhost:51001" };
        var domains = Array.Empty<string>();

        Assert.True(CorsOriginValidator.IsOriginAllowed("http://localhost:51000", origins, domains));
    }

    [Fact]
    public void WildcardSubdomain_Allowed()
    {
        var origins = Array.Empty<string>();
        var domains = new[] { "zenpharm.com.au" };

        Assert.True(CorsOriginValidator.IsOriginAllowed("https://smithpharmacy.zenpharm.com.au", origins, domains));
    }

    [Fact]
    public void UnknownDomain_Rejected()
    {
        var origins = new[] { "http://localhost:51000" };
        var domains = new[] { "zenpharm.com.au" };

        Assert.False(CorsOriginValidator.IsOriginAllowed("https://evil.example.com", origins, domains));
    }

    [Fact]
    public void ExactDomain_NotMatchedAsSubdomain()
    {
        // "zenpharm.com.au" itself (without subdomain) should NOT match
        // because the check is .EndsWith(".zenpharm.com.au")
        var origins = Array.Empty<string>();
        var domains = new[] { "zenpharm.com.au" };

        Assert.False(CorsOriginValidator.IsOriginAllowed("https://zenpharm.com.au", origins, domains));
    }

    [Fact]
    public void ExplicitOrigin_CaseInsensitive()
    {
        var origins = new[] { "http://LOCALHOST:51000" };
        var domains = Array.Empty<string>();

        Assert.True(CorsOriginValidator.IsOriginAllowed("http://localhost:51000", origins, domains));
    }

    [Fact]
    public void InvalidUri_Rejected()
    {
        var origins = Array.Empty<string>();
        var domains = new[] { "zenpharm.com.au" };

        Assert.False(CorsOriginValidator.IsOriginAllowed("not-a-url", origins, domains));
    }
}
