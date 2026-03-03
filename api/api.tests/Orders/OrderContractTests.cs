using Api.Features.Orders;
using Xunit;

namespace Api.Tests.Orders;

public class OrderContractTests
{
    [Fact]
    public void CreateGuestOrderRequest_DefaultValues()
    {
        var req = new CreateGuestOrderRequest();

        Assert.Equal("", req.FirstName);
        Assert.Equal("", req.LastName);
        Assert.Equal("", req.Email);
        Assert.Equal("", req.Phone);
        Assert.Null(req.Notes);
        Assert.Empty(req.Items);
    }

    [Fact]
    public void CreateGuestOrderRequest_WithValues()
    {
        var items = new[]
        {
            new OrderItemRequest { ProductId = Guid.NewGuid(), Quantity = 2 },
            new OrderItemRequest { ProductId = Guid.NewGuid(), Quantity = 1 },
        };

        var req = new CreateGuestOrderRequest
        {
            FirstName = "Jane",
            LastName = "Smith",
            Email = "jane@example.com",
            Phone = "0400111222",
            Notes = "Please have ready by 3pm",
            Items = items
        };

        Assert.Equal("Jane", req.FirstName);
        Assert.Equal("jane@example.com", req.Email);
        Assert.Equal(2, req.Items.Length);
        Assert.Equal(2, req.Items[0].Quantity);
    }

    [Fact]
    public void OrderItemRequest_DefaultValues()
    {
        var req = new OrderItemRequest();

        Assert.Equal(Guid.Empty, req.ProductId);
        Assert.Equal(0, req.Quantity);
    }

    [Fact]
    public void OrderDto_RecordEquality()
    {
        var id = Guid.NewGuid();
        var clientId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var items = new List<OrderItemDto>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), "Paracetamol", 2, 5.99m, 11.98m)
        };

        var a = new OrderDto(id, "ORD-20260303-0001", clientId, "Jane Smith",
            "pending", 11.98m, 1.20m, 13.18m, null, now, null, null, null, null, now, items);
        var b = new OrderDto(id, "ORD-20260303-0001", clientId, "Jane Smith",
            "pending", 11.98m, 1.20m, 13.18m, null, now, null, null, null, null, now, items);

        Assert.Equal(a, b);
    }

    [Fact]
    public void OrderItemDto_RecordEquality()
    {
        var id = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var a = new OrderItemDto(id, productId, "Paracetamol 500mg", 3, 5.99m, 17.97m);
        var b = new OrderItemDto(id, productId, "Paracetamol 500mg", 3, 5.99m, 17.97m);

        Assert.Equal(a, b);
    }

    [Fact]
    public void OrderListResponse_EmptyItems()
    {
        var response = new OrderListResponse([], 0);

        Assert.Empty(response.Items);
        Assert.Equal(0, response.TotalCount);
    }

    [Fact]
    public void OrderSummaryDto_Properties()
    {
        var now = DateTimeOffset.UtcNow;
        var dto = new OrderSummaryDto(Guid.NewGuid(), "ORD-20260303-0001",
            "Jane Smith", "pending", 3, 45.99m, now, now);

        Assert.Equal("ORD-20260303-0001", dto.OrderNumber);
        Assert.Equal("Jane Smith", dto.ClientName);
        Assert.Equal("pending", dto.Status);
        Assert.Equal(3, dto.ItemCount);
        Assert.Equal(45.99m, dto.Total);
    }

    [Fact]
    public void CancelOrderRequest_DefaultValues()
    {
        var req = new CancelOrderRequest();

        Assert.Equal("", req.Reason);
    }

    [Fact]
    public void CancelOrderRequest_WithReason()
    {
        var req = new CancelOrderRequest { Reason = "Customer changed their mind" };

        Assert.Equal("Customer changed their mind", req.Reason);
    }
}
