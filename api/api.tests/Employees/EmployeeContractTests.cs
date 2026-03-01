using Api.Features.Employees;
using Xunit;

namespace Api.Tests.Employees;

public class EmployeeContractTests
{
    [Fact]
    public void CreateEmployeeRequest_DefaultValues()
    {
        var req = new CreateEmployeeRequest();

        Assert.Equal("", req.FirstName);
        Assert.Equal("", req.LastName);
        Assert.Null(req.Email);
        Assert.Null(req.Phone);
        Assert.Equal("pharmacy_assistant", req.Role);
        Assert.True(req.IsActive);
    }

    [Fact]
    public void UpdateEmployeeRequest_DefaultValues()
    {
        var req = new UpdateEmployeeRequest();

        Assert.Equal("", req.FirstName);
        Assert.Equal("", req.LastName);
        Assert.Null(req.Email);
        Assert.Null(req.Phone);
        Assert.Equal("pharmacy_assistant", req.Role);
        Assert.True(req.IsActive);
    }

    [Fact]
    public void EmployeeDto_RecordEquality()
    {
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var a = new EmployeeDto(id, "Jane", "Doe", "jane@example.com", "0400111222", "manager", true, now);
        var b = new EmployeeDto(id, "Jane", "Doe", "jane@example.com", "0400111222", "manager", true, now);

        Assert.Equal(a, b);
    }

    [Fact]
    public void EmployeeDto_DifferentId_NotEqual()
    {
        var now = DateTimeOffset.UtcNow;
        var a = new EmployeeDto(Guid.NewGuid(), "Jane", "Doe", null, null, "pharmacist", true, now);
        var b = new EmployeeDto(Guid.NewGuid(), "Jane", "Doe", null, null, "pharmacist", true, now);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void EmployeeListResponse_EmptyItems()
    {
        var response = new EmployeeListResponse([], 0);

        Assert.Empty(response.Items);
        Assert.Equal(0, response.TotalCount);
    }

    [Fact]
    public void EmployeeListResponse_WithItems()
    {
        var items = new List<EmployeeDto>
        {
            new(Guid.NewGuid(), "Jane", "Doe", null, null, "pharmacist", true, DateTimeOffset.UtcNow),
            new(Guid.NewGuid(), "John", "Smith", "john@test.com", null, "dispense_technician", true, DateTimeOffset.UtcNow)
        };

        var response = new EmployeeListResponse(items, 2);

        Assert.Equal(2, response.Items.Count);
        Assert.Equal(2, response.TotalCount);
    }

    [Fact]
    public void CreateEmployeeRequest_PharmacyRoles()
    {
        var pharmacist = new CreateEmployeeRequest { FirstName = "Dr", LastName = "Smith", Role = "pharmacist" };
        var technician = new CreateEmployeeRequest { FirstName = "Jane", LastName = "Doe", Role = "dispense_technician" };
        var cashier = new CreateEmployeeRequest { FirstName = "Bob", LastName = "Lee", Role = "cashier" };

        Assert.Equal("pharmacist", pharmacist.Role);
        Assert.Equal("dispense_technician", technician.Role);
        Assert.Equal("cashier", cashier.Role);
    }
}
