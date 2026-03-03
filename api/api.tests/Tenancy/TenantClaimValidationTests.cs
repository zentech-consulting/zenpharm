using System.Security.Claims;
using Api.Common.Tenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Api.Tests.Tenancy;

public class TenantClaimValidationTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    private static readonly TenantContext ActiveTenant = new(
        TenantId: TenantId,
        Subdomain: "test",
        DisplayName: "Test Pharmacy",
        LogoUrl: null,
        PrimaryColour: "#1a1a2e",
        Plan: "Basic",
        Status: "Active",
        ConnectionString: "Server=localhost;Database=Test");

    private static TenantClaimValidationMiddleware CreateMiddleware(RequestDelegate? next = null)
        => new(
            next ?? (_ => Task.CompletedTask),
            NullLogger<TenantClaimValidationMiddleware>.Instance);

    private static HttpContext CreateContext(
        bool isAuthenticated = true,
        TenantContext? tenant = null,
        string? tenantIdClaim = null)
    {
        var ctx = new DefaultHttpContext();

        if (isAuthenticated)
        {
            var claims = new List<Claim>
            {
                new("sub", Guid.NewGuid().ToString()),
                new(ClaimTypes.Role, "Admin")
            };

            if (tenantIdClaim is not null)
                claims.Add(new Claim("tenant_id", tenantIdClaim));

            ctx.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));
        }

        if (tenant is not null)
            ctx.Items["TenantContext"] = tenant;

        return ctx;
    }

    [Fact]
    public async Task MatchingTenantId_Passes()
    {
        var nextCalled = false;
        var middleware = CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
        var ctx = CreateContext(
            isAuthenticated: true,
            tenant: ActiveTenant,
            tenantIdClaim: TenantId.ToString());

        await middleware.InvokeAsync(ctx);

        Assert.True(nextCalled);
        Assert.Equal(200, ctx.Response.StatusCode);
    }

    [Fact]
    public async Task MismatchedTenantId_Returns403()
    {
        var nextCalled = false;
        var middleware = CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
        var differentTenantId = Guid.NewGuid();
        var ctx = CreateContext(
            isAuthenticated: true,
            tenant: ActiveTenant,
            tenantIdClaim: differentTenantId.ToString());

        await middleware.InvokeAsync(ctx);

        Assert.False(nextCalled);
        Assert.Equal(403, ctx.Response.StatusCode);
    }

    [Fact]
    public async Task NoTenantIdClaim_PassesForBackwardsCompat()
    {
        var nextCalled = false;
        var middleware = CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
        var ctx = CreateContext(
            isAuthenticated: true,
            tenant: ActiveTenant,
            tenantIdClaim: null);

        await middleware.InvokeAsync(ctx);

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task AnonymousRequest_Passes()
    {
        var nextCalled = false;
        var middleware = CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
        var ctx = CreateContext(
            isAuthenticated: false,
            tenant: ActiveTenant);

        await middleware.InvokeAsync(ctx);

        Assert.True(nextCalled);
    }
}
