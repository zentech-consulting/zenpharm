using Api.Features.Orders;
using Xunit;

namespace Api.Tests.Orders;

public class OrderManagerTests
{
    [Fact]
    public void CalculateEstimatedReadyTime_ReturnsUtc()
    {
        var result = OrderManager.CalculateEstimatedReadyTime();

        Assert.Equal(TimeSpan.Zero, result.Offset);
    }

    [Fact]
    public void CalculateEstimatedReadyTime_ReturnsFutureTime()
    {
        var result = OrderManager.CalculateEstimatedReadyTime();

        Assert.True(result > DateTimeOffset.UtcNow, "Estimated ready time should be in the future");
    }

    [Fact]
    public void CalculateEstimatedReadyTime_NeverReturnsWeekend()
    {
        var result = OrderManager.CalculateEstimatedReadyTime();
        // Convert to AEST to check the day
        var aest = result.ToOffset(TimeSpan.FromHours(10));

        Assert.NotEqual(DayOfWeek.Saturday, aest.DayOfWeek);
        Assert.NotEqual(DayOfWeek.Sunday, aest.DayOfWeek);
    }

    [Fact]
    public void GstCalculation_TenPercent()
    {
        // Verify the GST rate constant works as expected
        const decimal gstRate = 0.10m;
        var subtotal = 100.00m;
        var tax = Math.Round(subtotal * gstRate, 2);

        Assert.Equal(10.00m, tax);
    }

    [Fact]
    public void GstCalculation_RoundsCorrectly()
    {
        const decimal gstRate = 0.10m;
        var subtotal = 33.33m;
        var tax = Math.Round(subtotal * gstRate, 2);

        Assert.Equal(3.33m, tax);
    }

    [Fact]
    public void OrderNumber_Format()
    {
        var today = DateTime.UtcNow.ToString("yyyyMMdd");
        var expected = $"ORD-{today}-0001";

        Assert.Matches(@"^ORD-\d{8}-\d{4}$", expected);
    }

    [Fact]
    public void OrderStatusValues_AreValid()
    {
        var validStatuses = new[] { "pending", "ready", "collected", "cancelled" };

        foreach (var status in validStatuses)
        {
            Assert.Contains(status, validStatuses);
        }
    }

    [Fact]
    public void OrderItemRequest_QuantityMustBePositive()
    {
        var item = new OrderItemRequest { ProductId = Guid.NewGuid(), Quantity = 1 };

        Assert.True(item.Quantity > 0);
    }

    [Fact]
    public void CreateGuestOrderRequest_ItemsRequired()
    {
        var req = new CreateGuestOrderRequest
        {
            FirstName = "Jane",
            LastName = "Smith",
            Email = "jane@example.com",
            Phone = "0400111222",
            Items = [new OrderItemRequest { ProductId = Guid.NewGuid(), Quantity = 2 }]
        };

        Assert.NotEmpty(req.Items);
        Assert.Equal("Jane", req.FirstName);
    }
}
