using Api.Features.MasterProducts;
using Xunit;

namespace Api.Tests.MasterProducts;

public class MasterProductContractTests
{
    [Fact]
    public void CreateMasterProductRequest_DefaultValues()
    {
        var req = new CreateMasterProductRequest();

        Assert.Equal("", req.Sku);
        Assert.Equal("", req.Name);
        Assert.Equal("", req.Category);
        Assert.Null(req.Description);
        Assert.Equal(0m, req.UnitPrice);
        Assert.Equal("each", req.Unit);
        Assert.Null(req.GenericName);
        Assert.Null(req.Brand);
        Assert.Null(req.Barcode);
        Assert.Equal("Unscheduled", req.ScheduleClass);
        Assert.Null(req.PackSize);
        Assert.Null(req.ActiveIngredients);
        Assert.Null(req.Warnings);
        Assert.Null(req.PbsItemCode);
        Assert.Null(req.ImageUrl);
    }

    [Fact]
    public void CreateMasterProductRequest_WithPharmacyFields()
    {
        var req = new CreateMasterProductRequest
        {
            Sku = "PARA-500",
            Name = "Paracetamol 500mg",
            Category = "Pain Relief",
            UnitPrice = 5.99m,
            GenericName = "Paracetamol",
            Brand = "Panamax",
            Barcode = "9312345678901",
            ScheduleClass = "S2",
            PackSize = "20 tablets",
            ActiveIngredients = "Paracetamol 500mg",
            Warnings = "Do not exceed 8 tablets in 24 hours",
            PbsItemCode = "2622B"
        };

        Assert.Equal("PARA-500", req.Sku);
        Assert.Equal("S2", req.ScheduleClass);
        Assert.Equal("Panamax", req.Brand);
        Assert.Equal("2622B", req.PbsItemCode);
    }

    [Fact]
    public void UpdateMasterProductRequest_DefaultValues()
    {
        var req = new UpdateMasterProductRequest();

        Assert.Equal("", req.Name);
        Assert.Equal("", req.Category);
        Assert.Equal(0m, req.UnitPrice);
        Assert.Equal("each", req.Unit);
        Assert.Equal("Unscheduled", req.ScheduleClass);
        Assert.True(req.IsActive);
    }

    [Fact]
    public void MasterProductDto_RecordEquality()
    {
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var a = new MasterProductDto(id, "PARA-500", "Paracetamol 500mg", "Pain Relief", null, 5.99m, "each",
            "Paracetamol", "Panamax", "9312345678901", "S2", "20 tablets", "Paracetamol 500mg",
            "Do not exceed 8 tablets", "2622B", null, true, now);
        var b = new MasterProductDto(id, "PARA-500", "Paracetamol 500mg", "Pain Relief", null, 5.99m, "each",
            "Paracetamol", "Panamax", "9312345678901", "S2", "20 tablets", "Paracetamol 500mg",
            "Do not exceed 8 tablets", "2622B", null, true, now);

        Assert.Equal(a, b);
    }

    [Fact]
    public void MasterProductDto_DifferentId_NotEqual()
    {
        var now = DateTimeOffset.UtcNow;
        var a = new MasterProductDto(Guid.NewGuid(), "PARA-500", "Paracetamol", "Pain Relief", null, 5.99m, "each",
            null, null, null, "Unscheduled", null, null, null, null, null, true, now);
        var b = new MasterProductDto(Guid.NewGuid(), "PARA-500", "Paracetamol", "Pain Relief", null, 5.99m, "each",
            null, null, null, "Unscheduled", null, null, null, null, null, true, now);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void MasterProductListResponse_EmptyItems()
    {
        var response = new MasterProductListResponse([], 0);

        Assert.Empty(response.Items);
        Assert.Equal(0, response.TotalCount);
    }

    [Fact]
    public void MasterProductListResponse_WithItems()
    {
        var now = DateTimeOffset.UtcNow;
        var items = new List<MasterProductDto>
        {
            new(Guid.NewGuid(), "PARA-500", "Paracetamol 500mg", "Pain Relief", null, 5.99m, "each",
                "Paracetamol", "Panamax", null, "S2", null, null, null, null, null, true, now),
            new(Guid.NewGuid(), "IBUP-200", "Ibuprofen 200mg", "Pain Relief", null, 7.99m, "each",
                "Ibuprofen", "Nurofen", null, "S2", null, null, null, null, null, true, now)
        };

        var response = new MasterProductListResponse(items, 2);

        Assert.Equal(2, response.Items.Count);
        Assert.Equal(2, response.TotalCount);
    }

    [Fact]
    public void MasterProductDto_ScheduleClassValues()
    {
        var now = DateTimeOffset.UtcNow;
        var unscheduled = new MasterProductDto(Guid.NewGuid(), "VIT-C", "Vitamin C", "Vitamins", null, 9.99m, "each",
            null, null, null, "Unscheduled", null, null, null, null, null, true, now);
        var s4 = new MasterProductDto(Guid.NewGuid(), "AMOX-500", "Amoxicillin 500mg", "Antibiotics", null, 12.99m, "each",
            "Amoxicillin", null, null, "S4", null, null, null, null, null, true, now);

        Assert.Equal("Unscheduled", unscheduled.ScheduleClass);
        Assert.Equal("S4", s4.ScheduleClass);
    }
}
