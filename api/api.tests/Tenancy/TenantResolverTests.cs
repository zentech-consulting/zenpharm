using System.Data;
using Api.Common;
using Api.Common.Tenancy;
using Dapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Api.Tests.Tenancy;

public class TenantResolverTests
{
    private readonly ICatalogDb _catalogDb = Substitute.For<ICatalogDb>();
    private readonly ILogger<TenantResolver> _logger = NullLogger<TenantResolver>.Instance;
    private readonly IDbConnection _mockConn = Substitute.For<IDbConnection>();

    private TenantResolver CreateResolver()
    {
        _catalogDb.CreateAsync().Returns(Task.FromResult(_mockConn));
        return new TenantResolver(_catalogDb, _logger);
    }

    [Fact]
    public async Task ResolveAsync_DbUnavailable_ReturnsNull()
    {
        // When the catalogue DB is unavailable, Dapper throws.
        // TenantResolver should handle this gracefully and return null.
        _catalogDb.CreateAsync().Returns<IDbConnection>(_ => throw new InvalidOperationException("DB unavailable"));

        var resolver = new TenantResolver(_catalogDb, _logger);

        var result = await resolver.ResolveAsync("nonexistent");

        Assert.Null(result);
    }

    [Fact]
    public void InvalidateCache_DoesNotThrow()
    {
        var resolver = CreateResolver();

        // Invalidating a non-existent cache entry should not throw
        resolver.InvalidateCache("unknown");
        resolver.InvalidateCache("unknown"); // idempotent
    }

    [Fact]
    public void TenantContext_IsActive_ActiveStatus_ReturnsTrue()
    {
        var ctx = new TenantContext(
            TenantId: Guid.NewGuid(),
            Subdomain: "test",
            DisplayName: "Test Tenant",
            LogoUrl: null,
            PrimaryColour: "#1890ff",
            Plan: "Basic",
            Status: "Active",
            ConnectionString: "Server=localhost;Database=Test");

        Assert.True(ctx.IsActive);
    }

    [Fact]
    public void TenantContext_IsActive_SuspendedStatus_ReturnsFalse()
    {
        var ctx = new TenantContext(
            TenantId: Guid.NewGuid(),
            Subdomain: "test",
            DisplayName: "Test Tenant",
            LogoUrl: null,
            PrimaryColour: "#1890ff",
            Plan: "Basic",
            Status: "Suspended",
            ConnectionString: "Server=localhost;Database=Test");

        Assert.False(ctx.IsActive);
    }

    [Fact]
    public void TenantContext_IsActive_CancelledStatus_ReturnsFalse()
    {
        var ctx = new TenantContext(
            TenantId: Guid.NewGuid(),
            Subdomain: "test",
            DisplayName: "Test Tenant",
            LogoUrl: null,
            PrimaryColour: "#1890ff",
            Plan: "Basic",
            Status: "Cancelled",
            ConnectionString: "Server=localhost;Database=Test");

        Assert.False(ctx.IsActive);
    }

    [Fact]
    public void TenantContext_IsActive_CaseInsensitive()
    {
        var ctx = new TenantContext(
            TenantId: Guid.NewGuid(),
            Subdomain: "test",
            DisplayName: "Test Tenant",
            LogoUrl: null,
            PrimaryColour: "#1890ff",
            Plan: "Basic",
            Status: "active",  // lowercase
            ConnectionString: "Server=localhost;Database=Test");

        Assert.True(ctx.IsActive);
    }

    [Fact]
    public void TenantContext_RecordEquality()
    {
        var id = Guid.NewGuid();
        var a = new TenantContext(id, "test", "Test", null, "#fff", "Basic", "Active", "conn");
        var b = new TenantContext(id, "test", "Test", null, "#fff", "Basic", "Active", "conn");

        Assert.Equal(a, b);
    }

    [Fact]
    public void TenantEntity_DefaultValues()
    {
        var entity = new TenantEntity();

        Assert.Equal("", entity.Subdomain);
        Assert.Equal("", entity.DisplayName);
        Assert.Equal("#1890ff", entity.PrimaryColour);
        Assert.Equal("", entity.ConnectionString);
        Assert.Equal("Active", entity.Status);
        Assert.Null(entity.LogoUrl);
        Assert.Null(entity.PlanName);
    }

    [Fact]
    public async Task SqlConnectionFactory_EmptyConnectionString_Throws()
    {
        var factory = new SqlConnectionFactory("");

        await Assert.ThrowsAsync<InvalidOperationException>(() => factory.CreateAsync());
    }

    [Fact]
    public async Task TenantSqlConnectionFactory_EmptyConnectionString_Throws()
    {
        var factory = new TenantSqlConnectionFactory("");

        await Assert.ThrowsAsync<InvalidOperationException>(() => factory.CreateAsync());
    }
}
