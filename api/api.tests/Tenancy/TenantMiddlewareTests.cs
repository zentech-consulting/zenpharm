using Api.Common.Tenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Api.Tests.Tenancy;

public class TenantMiddlewareTests
{
    private static readonly TenantContext ActiveTenant = new(
        TenantId: Guid.NewGuid(),
        Subdomain: "smithpharmacy",
        DisplayName: "Smith Pharmacy",
        LogoUrl: null,
        PrimaryColour: "#1a1a2e",
        Plan: "Basic",
        Status: "Active",
        ConnectionString: "Server=localhost;Database=Test");

    private static IConfiguration CreateConfig(string? devSubdomain = null)
    {
        var dict = new Dictionary<string, string?>();
        if (devSubdomain is not null)
            dict["Tenancy:DevTenantSubdomain"] = devSubdomain;

        return new ConfigurationBuilder()
            .AddInMemoryCollection(dict)
            .Build();
    }

    private static (TenantMiddleware, HttpContext) CreateMiddleware(
        RequestDelegate? next = null,
        ITenantResolver? resolver = null,
        IConfiguration? config = null,
        string? host = null,
        string? headerSubdomain = null,
        string path = "/api/test")
    {
        next ??= _ => Task.CompletedTask;
        resolver ??= Substitute.For<ITenantResolver>();
        config ??= CreateConfig("dev");

        var ctx = new DefaultHttpContext();
        ctx.Request.Path = path;
        ctx.Request.Host = new HostString(host ?? "localhost");

        if (headerSubdomain is not null)
            ctx.Request.Headers[TenantMiddleware.TenantSubdomainHeader] = headerSubdomain;

        var middleware = new TenantMiddleware(
            next, resolver, config,
            NullLogger<TenantMiddleware>.Instance);

        return (middleware, ctx);
    }

    [Fact]
    public async Task InvokeAsync_HeaderTakesPriorityOverHost()
    {
        var resolver = Substitute.For<ITenantResolver>();
        resolver.ResolveAsync("smithpharmacy", Arg.Any<CancellationToken>())
            .Returns(ActiveTenant);

        // Host would resolve to "dev" (localhost fallback), but header says "smithpharmacy"
        var (middleware, ctx) = CreateMiddleware(
            resolver: resolver,
            host: "localhost",
            headerSubdomain: "smithpharmacy",
            config: CreateConfig("dev"));

        await middleware.InvokeAsync(ctx);

        // Should have resolved using the header subdomain, not the Host fallback
        await resolver.Received(1).ResolveAsync("smithpharmacy", Arg.Any<CancellationToken>());
        Assert.NotNull(ctx.Items["TenantContext"]);
        Assert.Equal(ActiveTenant, ctx.Items["TenantContext"]);
    }

    [Fact]
    public async Task InvokeAsync_EmptyHeader_FallsBackToHost()
    {
        var resolver = Substitute.For<ITenantResolver>();
        resolver.ResolveAsync("dev", Arg.Any<CancellationToken>())
            .Returns(ActiveTenant);

        var (middleware, ctx) = CreateMiddleware(
            resolver: resolver,
            host: "localhost",
            headerSubdomain: "",
            config: CreateConfig("dev"));

        await middleware.InvokeAsync(ctx);

        // Should fall back to Host-based extraction (localhost → "dev" config)
        await resolver.Received(1).ResolveAsync("dev", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InvokeAsync_InvalidHeader_Returns400()
    {
        var resolver = Substitute.For<ITenantResolver>();

        var (middleware, ctx) = CreateMiddleware(
            resolver: resolver,
            headerSubdomain: "-invalid-subdomain-");

        await middleware.InvokeAsync(ctx);

        Assert.Equal(400, ctx.Response.StatusCode);
        await resolver.DidNotReceive().ResolveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InvokeAsync_NoHeader_UsesHostExtraction()
    {
        var resolver = Substitute.For<ITenantResolver>();
        resolver.ResolveAsync("demo", Arg.Any<CancellationToken>())
            .Returns(ActiveTenant);

        var (middleware, ctx) = CreateMiddleware(
            resolver: resolver,
            host: "demo.example.com",
            config: CreateConfig());

        await middleware.InvokeAsync(ctx);

        await resolver.Received(1).ResolveAsync("demo", Arg.Any<CancellationToken>());
    }
}
