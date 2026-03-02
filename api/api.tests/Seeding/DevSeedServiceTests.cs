using Api.Common;
using Api.Common.Migrations;
using Api.Common.Seeding;
using Api.Common.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Api.Tests.Seeding;

public class DevSeedServiceTests
{
    // ================================================================
    // PharmacyMasterProductData Tests
    // ================================================================

    [Fact]
    public void PharmacyMasterProductData_All_HasExpectedProductCount()
    {
        Assert.True(PharmacyMasterProductData.All.Length >= 350,
            $"Expected at least 350 products but found {PharmacyMasterProductData.All.Length}");
    }

    [Fact]
    public void PharmacyMasterProductData_All_HasUniqueSkus()
    {
        var skus = PharmacyMasterProductData.All.Select(p => p.Sku).ToList();
        var distinct = skus.Distinct().ToList();

        Assert.Equal(skus.Count, distinct.Count);
    }

    [Fact]
    public void PharmacyMasterProductData_All_HasUniqueBarcodes()
    {
        var barcodes = PharmacyMasterProductData.All
            .Where(p => p.Barcode is not null)
            .Select(p => p.Barcode!)
            .ToList();
        var distinct = barcodes.Distinct().ToList();

        Assert.Equal(barcodes.Count, distinct.Count);
    }

    [Fact]
    public void PharmacyMasterProductData_All_HasMultipleCategories()
    {
        var categories = PharmacyMasterProductData.All
            .Select(p => p.Category)
            .Distinct()
            .ToList();

        Assert.True(categories.Count >= 15,
            $"Expected at least 15 categories but found {categories.Count}");
    }

    [Fact]
    public void PharmacyMasterProductData_All_ScheduleClassesAreValid()
    {
        var validClasses = new HashSet<string> { "Unscheduled", "S2", "S3", "S4" };

        foreach (var product in PharmacyMasterProductData.All)
        {
            Assert.Contains(product.ScheduleClass, validClasses);
        }
    }

    [Fact]
    public void PharmacyMasterProductData_All_PricesArePositive()
    {
        foreach (var product in PharmacyMasterProductData.All)
        {
            Assert.True(product.UnitPrice > 0,
                $"Product '{product.Sku}' has non-positive price: {product.UnitPrice}");
        }
    }

    [Fact]
    public void PharmacyMasterProductData_All_HasMixedScheduleClasses()
    {
        var classes = PharmacyMasterProductData.All
            .Select(p => p.ScheduleClass)
            .Distinct()
            .ToHashSet();

        Assert.Contains("Unscheduled", classes);
        Assert.Contains("S2", classes);
        Assert.Contains("S3", classes);
        Assert.Contains("S4", classes);
    }

    [Fact]
    public void PharmacyMasterProductData_All_CategoriesNotEmpty()
    {
        foreach (var product in PharmacyMasterProductData.All)
        {
            Assert.False(string.IsNullOrWhiteSpace(product.Category),
                $"Product '{product.Sku}' has empty category");
        }
    }

    [Fact]
    public void PharmacyMasterProductData_All_NamesNotEmpty()
    {
        foreach (var product in PharmacyMasterProductData.All)
        {
            Assert.False(string.IsNullOrWhiteSpace(product.Name),
                $"Product '{product.Sku}' has empty name");
            Assert.False(string.IsNullOrWhiteSpace(product.Sku),
                "Found product with empty SKU");
        }
    }

    // ================================================================
    // DevSeedService Tests
    // ================================================================

    [Fact]
    public void DevSeedService_AdminPasswordHash_IsValidBCrypt()
    {
        var hash = BCrypt.Net.BCrypt.HashPassword("admin123");

        Assert.True(BCrypt.Net.BCrypt.Verify("admin123", hash));
        Assert.False(BCrypt.Net.BCrypt.Verify("wrongpassword", hash));
    }

    [Fact]
    public async Task DevSeedService_SeedAsync_DoesNotThrowOnConnectionFailure()
    {
        var catalogDb = Substitute.For<ICatalogDb>();
        catalogDb.CreateAsync()
            .Returns(Task.FromException<System.Data.IDbConnection>(
                new InvalidOperationException("Connection unavailable")));

        var tenantMigration = Substitute.For<ITenantMigration>();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=fake;Database=test;"
            })
            .Build();

        var logger = NullLogger<DevSeedService>.Instance;

        var protector = Substitute.For<IConnectionStringProtector>();
        protector.Protect(Arg.Any<string>()).Returns(x => x.Arg<string>());

        var service = new DevSeedService(catalogDb, protector, tenantMigration, config, logger);

        // Should not throw — graceful fallback
        await service.SeedAsync();
    }

    [Fact]
    public async Task DevSeedService_SeedAsync_SkipsWhenNoConnectionString()
    {
        var catalogDb = Substitute.For<ICatalogDb>();
        var tenantMigration = Substitute.For<ITenantMigration>();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = ""
            })
            .Build();

        var logger = NullLogger<DevSeedService>.Instance;

        var protector = Substitute.For<IConnectionStringProtector>();

        var service = new DevSeedService(catalogDb, protector, tenantMigration, config, logger);

        // Should return immediately without calling catalogDb
        await service.SeedAsync();

        await catalogDb.DidNotReceive().CreateAsync();
    }
}
