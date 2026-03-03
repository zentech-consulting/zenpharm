using Api.Features.Shop;
using Xunit;

namespace Api.Tests.Shop;

public class ShopContractTests
{
    [Fact]
    public void ShopProductDto_RecordEquality()
    {
        var id = Guid.NewGuid();
        var a = new ShopProductDto(id, "Paracetamol 500mg", "Paracetamol", "Panamax",
            "Pain Relief", "S2", 5.99m, null, "In Stock", true);
        var b = new ShopProductDto(id, "Paracetamol 500mg", "Paracetamol", "Panamax",
            "Pain Relief", "S2", 5.99m, null, "In Stock", true);

        Assert.Equal(a, b);
    }

    [Fact]
    public void ShopProductDto_DifferentId_NotEqual()
    {
        var a = new ShopProductDto(Guid.NewGuid(), "Paracetamol", null, null,
            "Pain Relief", "S2", 5.99m, null, "In Stock", false);
        var b = new ShopProductDto(Guid.NewGuid(), "Paracetamol", null, null,
            "Pain Relief", "S2", 5.99m, null, "In Stock", false);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void ShopProductDetailDto_IncludesExtendedFields()
    {
        var dto = new ShopProductDetailDto(Guid.NewGuid(), "Paracetamol 500mg", "Paracetamol",
            "Panamax", "Pain Relief", "S2", 5.99m, null, "In Stock", false,
            "Paracetamol 500mg", "Do not exceed 4g daily", "Pain relief tablets");

        Assert.Equal("Paracetamol 500mg", dto.ActiveIngredients);
        Assert.Equal("Do not exceed 4g daily", dto.Warnings);
        Assert.Equal("Pain relief tablets", dto.Description);
    }

    [Fact]
    public void ShopProductListResponse_EmptyItems()
    {
        var response = new ShopProductListResponse([], 0);

        Assert.Empty(response.Items);
        Assert.Equal(0, response.TotalCount);
    }

    [Fact]
    public void ShopProductListResponse_WithItems()
    {
        var items = new List<ShopProductDto>
        {
            new(Guid.NewGuid(), "Paracetamol", null, null, "Pain Relief", "S2",
                5.99m, null, "In Stock", true),
            new(Guid.NewGuid(), "Ibuprofen", null, null, "Pain Relief", "S2",
                7.99m, null, "Low Stock", false),
        };

        var response = new ShopProductListResponse(items, 2);

        Assert.Equal(2, response.Items.Count);
        Assert.Equal(2, response.TotalCount);
    }
}
