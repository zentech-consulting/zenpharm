using Api.Features.Products;
using Xunit;

namespace Api.Tests.Products;

public class ProductContractTests
{
    [Fact]
    public void ProductDto_RecordEquality()
    {
        var id = Guid.NewGuid();
        var mpId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var expiry = new DateOnly(2026, 12, 31);
        var a = new ProductDto(id, mpId, "Paracetamol 500mg", "Paracetamol", "Panamax", "Pain Relief", "S2",
            5.99m, null, null, null, 100, 10, expiry, true, false, 0, now);
        var b = new ProductDto(id, mpId, "Paracetamol 500mg", "Paracetamol", "Panamax", "Pain Relief", "S2",
            5.99m, null, null, null, 100, 10, expiry, true, false, 0, now);

        Assert.Equal(a, b);
    }

    [Fact]
    public void ProductDto_DifferentId_NotEqual()
    {
        var mpId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var a = new ProductDto(Guid.NewGuid(), mpId, "Paracetamol", null, null, "Pain Relief", "S2",
            5.99m, null, null, null, 50, 10, null, true, false, 0, now);
        var b = new ProductDto(Guid.NewGuid(), mpId, "Paracetamol", null, null, "Pain Relief", "S2",
            5.99m, null, null, null, 50, 10, null, true, false, 0, now);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void ProductDto_WithCustomNameAndPrice()
    {
        var dto = new ProductDto(Guid.NewGuid(), Guid.NewGuid(), "Paracetamol 500mg", null, null,
            "Pain Relief", "S2", 5.99m, "Our Paracetamol", 4.99m, null, 200, 20, null, true, true, 1,
            DateTimeOffset.UtcNow);

        Assert.Equal("Our Paracetamol", dto.CustomName);
        Assert.Equal(4.99m, dto.CustomPrice);
        Assert.True(dto.IsFeatured);
        Assert.Equal(1, dto.SortOrder);
    }

    [Fact]
    public void ProductListResponse_EmptyItems()
    {
        var response = new ProductListResponse([], 0);

        Assert.Empty(response.Items);
        Assert.Equal(0, response.TotalCount);
    }

    [Fact]
    public void ProductListResponse_WithItems()
    {
        var now = DateTimeOffset.UtcNow;
        var items = new List<ProductDto>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), "Paracetamol", null, null, "Pain Relief", "S2",
                5.99m, null, null, null, 100, 10, null, true, false, 0, now),
            new(Guid.NewGuid(), Guid.NewGuid(), "Ibuprofen", null, null, "Pain Relief", "S2",
                7.99m, null, null, null, 50, 10, null, true, false, 0, now)
        };

        var response = new ProductListResponse(items, 2);

        Assert.Equal(2, response.Items.Count);
        Assert.Equal(2, response.TotalCount);
    }

    [Fact]
    public void ImportProductsRequest_DefaultValues()
    {
        var req = new ImportProductsRequest();

        Assert.Empty(req.MasterProductIds);
    }

    [Fact]
    public void ImportProductsRequest_WithIds()
    {
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var req = new ImportProductsRequest { MasterProductIds = ids };

        Assert.Equal(3, req.MasterProductIds.Length);
    }

    [Fact]
    public void UpdateProductRequest_DefaultValues()
    {
        var req = new UpdateProductRequest();

        Assert.Null(req.CustomName);
        Assert.Null(req.CustomPrice);
        Assert.Equal(10, req.ReorderLevel);
        Assert.Null(req.ExpiryDate);
        Assert.True(req.IsVisible);
        Assert.False(req.IsFeatured);
        Assert.Equal(0, req.SortOrder);
    }

    [Fact]
    public void RecordStockMovementRequest_DefaultValues()
    {
        var req = new RecordStockMovementRequest();

        Assert.Equal("stock_in", req.MovementType);
        Assert.Equal(0, req.Quantity);
        Assert.Null(req.Reference);
        Assert.Null(req.Notes);
        Assert.Null(req.CreatedBy);
    }

    [Fact]
    public void RecordStockMovementRequest_WithValues()
    {
        var req = new RecordStockMovementRequest
        {
            MovementType = "stock_out",
            Quantity = 5,
            Reference = "SALE-001",
            Notes = "Customer purchase",
            CreatedBy = "admin@pharmacy.com"
        };

        Assert.Equal("stock_out", req.MovementType);
        Assert.Equal(5, req.Quantity);
        Assert.Equal("SALE-001", req.Reference);
    }

    [Fact]
    public void StockMovementDto_RecordEquality()
    {
        var id = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var a = new StockMovementDto(id, productId, "stock_in", 50, "PO-123", "Initial stock", now, "admin", null);
        var b = new StockMovementDto(id, productId, "stock_in", 50, "PO-123", "Initial stock", now, "admin", null);

        Assert.Equal(a, b);
    }

    [Fact]
    public void StockMovementListResponse_EmptyItems()
    {
        var response = new StockMovementListResponse([], 0);

        Assert.Empty(response.Items);
        Assert.Equal(0, response.TotalCount);
    }

    [Fact]
    public void LowStockSummary_EmptyItems()
    {
        var summary = new LowStockSummary(10, 0, []);

        Assert.Equal(10, summary.TotalProducts);
        Assert.Equal(0, summary.LowStockCount);
        Assert.Empty(summary.LowStockItems);
    }

    [Fact]
    public void ExpiryAlertResponse_WithItems()
    {
        var now = DateTimeOffset.UtcNow;
        var expiringItem = new ProductDto(Guid.NewGuid(), Guid.NewGuid(), "Aspirin", null, null,
            "Pain Relief", "S2", 3.99m, null, null, null, 20, 10,
            new DateOnly(2026, 4, 1), true, false, 0, now);

        var response = new ExpiryAlertResponse(1, [expiringItem]);

        Assert.Equal(1, response.ExpiringCount);
        Assert.Single(response.ExpiringItems);
        Assert.Equal(new DateOnly(2026, 4, 1), response.ExpiringItems[0].ExpiryDate);
    }
}
