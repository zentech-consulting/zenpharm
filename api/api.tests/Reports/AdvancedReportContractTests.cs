using Api.Features.Reports;
using Xunit;

namespace Api.Tests.Reports;

public class AdvancedReportContractTests
{
    // ================================================================
    // TopSellingProductDto / Report
    // ================================================================

    [Fact]
    public void TopSellingProductDto_RecordEquality()
    {
        var id = Guid.NewGuid();
        var a = new TopSellingProductDto(id, "Paracetamol", "Panamax", "Pain Relief", 50, 299.50m);
        var b = new TopSellingProductDto(id, "Paracetamol", "Panamax", "Pain Relief", 50, 299.50m);

        Assert.Equal(a, b);
    }

    [Fact]
    public void TopSellingProductDto_DifferentQuantity_NotEqual()
    {
        var id = Guid.NewGuid();
        var a = new TopSellingProductDto(id, "Paracetamol", "Panamax", "Pain Relief", 50, 299.50m);
        var b = new TopSellingProductDto(id, "Paracetamol", "Panamax", "Pain Relief", 100, 599.00m);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void TopSellingProductsReport_EmptyItems()
    {
        var report = new TopSellingProductsReport([], 0);

        Assert.Empty(report.Items);
        Assert.Equal(0, report.TotalStockOutMovements);
    }

    [Fact]
    public void TopSellingProductsReport_WithItems()
    {
        var items = new List<TopSellingProductDto>
        {
            new(Guid.NewGuid(), "Paracetamol", "Panamax", "Pain Relief", 50, 299.50m),
            new(Guid.NewGuid(), "Ibuprofen", "Nurofen", "Pain Relief", 30, 239.70m)
        };

        var report = new TopSellingProductsReport(items, 80);

        Assert.Equal(2, report.Items.Count);
        Assert.Equal(80, report.TotalStockOutMovements);
    }

    // ================================================================
    // RevenueByCategoryDto / Report
    // ================================================================

    [Fact]
    public void RevenueByCategoryDto_RecordEquality()
    {
        var a = new RevenueByCategoryDto("Consultation", 25, 1250.00m);
        var b = new RevenueByCategoryDto("Consultation", 25, 1250.00m);

        Assert.Equal(a, b);
    }

    [Fact]
    public void RevenueByCategoryReport_EmptyItems()
    {
        var report = new RevenueByCategoryReport([], 0m);

        Assert.Empty(report.Items);
        Assert.Equal(0m, report.TotalRevenue);
    }

    [Fact]
    public void RevenueByCategoryReport_WithItems()
    {
        var items = new List<RevenueByCategoryDto>
        {
            new("Consultation", 25, 1250.00m),
            new("Vaccination", 10, 500.00m)
        };

        var report = new RevenueByCategoryReport(items, 1750.00m);

        Assert.Equal(2, report.Items.Count);
        Assert.Equal(1750.00m, report.TotalRevenue);
    }

    // ================================================================
    // ExpiryWasteDto / Report
    // ================================================================

    [Fact]
    public void ExpiryWasteDto_RecordEquality()
    {
        var id = Guid.NewGuid();
        var a = new ExpiryWasteDto(id, "Aspirin", "Bayer", 20, 79.80m);
        var b = new ExpiryWasteDto(id, "Aspirin", "Bayer", 20, 79.80m);

        Assert.Equal(a, b);
    }

    [Fact]
    public void ExpiryWasteReport_EmptyItems()
    {
        var report = new ExpiryWasteReport([], 0, 0m);

        Assert.Empty(report.Items);
        Assert.Equal(0, report.TotalExpiredMovements);
        Assert.Equal(0m, report.TotalWasteValue);
    }

    [Fact]
    public void ExpiryWasteReport_WithItems()
    {
        var items = new List<ExpiryWasteDto>
        {
            new(Guid.NewGuid(), "Aspirin", "Bayer", 20, 79.80m),
            new(Guid.NewGuid(), "Vitamin C", null, 10, 49.90m)
        };

        var report = new ExpiryWasteReport(items, 30, 129.70m);

        Assert.Equal(2, report.Items.Count);
        Assert.Equal(30, report.TotalExpiredMovements);
        Assert.Equal(129.70m, report.TotalWasteValue);
    }

    // ================================================================
    // EmployeeUtilisationDto / Report
    // ================================================================

    [Fact]
    public void EmployeeUtilisationDto_RecordEquality()
    {
        var id = Guid.NewGuid();
        var a = new EmployeeUtilisationDto(id, "Jane Smith", "Pharmacist", 30, 25, 1250.00m);
        var b = new EmployeeUtilisationDto(id, "Jane Smith", "Pharmacist", 30, 25, 1250.00m);

        Assert.Equal(a, b);
    }

    [Fact]
    public void EmployeeUtilisationDto_DifferentBookings_NotEqual()
    {
        var id = Guid.NewGuid();
        var a = new EmployeeUtilisationDto(id, "Jane Smith", "Pharmacist", 30, 25, 1250.00m);
        var b = new EmployeeUtilisationDto(id, "Jane Smith", "Pharmacist", 40, 35, 1750.00m);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void EmployeeUtilisationReport_EmptyItems()
    {
        var report = new EmployeeUtilisationReport([], 0);

        Assert.Empty(report.Items);
        Assert.Equal(0, report.TotalBookings);
    }

    [Fact]
    public void EmployeeUtilisationReport_WithItems()
    {
        var items = new List<EmployeeUtilisationDto>
        {
            new(Guid.NewGuid(), "Jane Smith", "Pharmacist", 30, 25, 1250.00m),
            new(Guid.NewGuid(), "John Doe", "Technician", 20, 18, 900.00m)
        };

        var report = new EmployeeUtilisationReport(items, 50);

        Assert.Equal(2, report.Items.Count);
        Assert.Equal(50, report.TotalBookings);
    }
}
