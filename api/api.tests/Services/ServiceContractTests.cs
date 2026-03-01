using Api.Features.Services;
using Xunit;

namespace Api.Tests.Services;

public class ServiceContractTests
{
    [Fact]
    public void CreateServiceRequest_DefaultValues()
    {
        var req = new CreateServiceRequest();

        Assert.Equal("", req.Name);
        Assert.Null(req.Description);
        Assert.Equal("", req.Category);
        Assert.Equal(0m, req.Price);
        Assert.Equal(30, req.DurationMinutes);
        Assert.True(req.IsActive);
    }

    [Fact]
    public void UpdateServiceRequest_DefaultValues()
    {
        var req = new UpdateServiceRequest();

        Assert.Equal("", req.Name);
        Assert.Null(req.Description);
        Assert.Equal("", req.Category);
        Assert.Equal(0m, req.Price);
        Assert.Equal(30, req.DurationMinutes);
        Assert.True(req.IsActive);
    }

    [Fact]
    public void ServiceDto_RecordEquality()
    {
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var a = new ServiceDto(id, "Haircut", "Basic cut", "Grooming", 25.00m, 30, true, now);
        var b = new ServiceDto(id, "Haircut", "Basic cut", "Grooming", 25.00m, 30, true, now);

        Assert.Equal(a, b);
    }

    [Fact]
    public void ServiceDto_DifferentId_NotEqual()
    {
        var now = DateTimeOffset.UtcNow;
        var a = new ServiceDto(Guid.NewGuid(), "Haircut", null, "Grooming", 25.00m, 30, true, now);
        var b = new ServiceDto(Guid.NewGuid(), "Haircut", null, "Grooming", 25.00m, 30, true, now);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void ServiceListResponse_EmptyItems()
    {
        var response = new ServiceListResponse([], 0);

        Assert.Empty(response.Items);
        Assert.Equal(0, response.TotalCount);
    }

    [Fact]
    public void ServiceListResponse_WithItems()
    {
        var items = new List<ServiceDto>
        {
            new(Guid.NewGuid(), "Haircut", null, "Grooming", 25.00m, 30, true, DateTimeOffset.UtcNow),
            new(Guid.NewGuid(), "Colour", "Full colour treatment", "Colouring", 80.00m, 60, true, DateTimeOffset.UtcNow)
        };

        var response = new ServiceListResponse(items, 2);

        Assert.Equal(2, response.Items.Count);
        Assert.Equal(2, response.TotalCount);
    }
}
