using Api.Features.Shop;
using Xunit;

namespace Api.Tests.Shop;

public class ShopManagerTests
{
    [Theory]
    [InlineData(0, "Out of Stock")]
    [InlineData(-1, "Out of Stock")]
    [InlineData(1, "Low Stock")]
    [InlineData(5, "Low Stock")]
    [InlineData(10, "Low Stock")]
    [InlineData(11, "In Stock")]
    [InlineData(100, "In Stock")]
    public void MapStockAvailability_ReturnsCorrectText(int quantity, string expected)
    {
        var result = ShopManager.MapStockAvailability(quantity);

        Assert.Equal(expected, result);
    }
}
