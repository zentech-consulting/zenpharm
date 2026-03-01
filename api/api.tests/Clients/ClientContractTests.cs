using Api.Features.Clients;
using Xunit;

namespace Api.Tests.Clients;

public class ClientContractTests
{
    [Fact]
    public void CreateClientRequest_DefaultValues()
    {
        var req = new CreateClientRequest();

        Assert.Equal("", req.FirstName);
        Assert.Equal("", req.LastName);
        Assert.Null(req.Email);
        Assert.Null(req.Phone);
        Assert.Null(req.Notes);
    }

    [Fact]
    public void UpdateClientRequest_DefaultValues()
    {
        var req = new UpdateClientRequest();

        Assert.Equal("", req.FirstName);
        Assert.Equal("", req.LastName);
        Assert.Null(req.Email);
        Assert.Null(req.Phone);
        Assert.Null(req.Notes);
    }

    [Fact]
    public void ClientDto_RecordEquality()
    {
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var a = new ClientDto(id, "Alice", "Smith", "alice@example.com", "0400000000", null, now);
        var b = new ClientDto(id, "Alice", "Smith", "alice@example.com", "0400000000", null, now);

        Assert.Equal(a, b);
    }

    [Fact]
    public void ClientDto_DifferentId_NotEqual()
    {
        var now = DateTimeOffset.UtcNow;
        var a = new ClientDto(Guid.NewGuid(), "Alice", "Smith", null, null, null, now);
        var b = new ClientDto(Guid.NewGuid(), "Alice", "Smith", null, null, null, now);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void ClientListResponse_EmptyItems()
    {
        var response = new ClientListResponse([], 0);

        Assert.Empty(response.Items);
        Assert.Equal(0, response.TotalCount);
    }

    [Fact]
    public void ClientListResponse_WithItems()
    {
        var items = new List<ClientDto>
        {
            new(Guid.NewGuid(), "Alice", "Smith", null, null, null, DateTimeOffset.UtcNow),
            new(Guid.NewGuid(), "Bob", "Jones", "bob@test.com", null, null, DateTimeOffset.UtcNow)
        };

        var response = new ClientListResponse(items, 2);

        Assert.Equal(2, response.Items.Count);
        Assert.Equal(2, response.TotalCount);
    }
}
