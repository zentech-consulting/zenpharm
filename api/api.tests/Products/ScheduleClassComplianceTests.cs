using Api.Features.Products;
using Xunit;

namespace Api.Tests.Products;

public class ScheduleClassComplianceTests
{
    // ================================================================
    // S2 — Pharmacy Medicine: no special approval needed
    // ================================================================

    [Fact]
    public void ValidateScheduleClassCompliance_S2_NoApproval_DoesNotThrow()
    {
        var ex = Record.Exception(() =>
            ProductManager.ValidateScheduleClassCompliance("S2", null));

        Assert.Null(ex);
    }

    [Fact]
    public void ValidateScheduleClassCompliance_S2_WithApproval_DoesNotThrow()
    {
        var ex = Record.Exception(() =>
            ProductManager.ValidateScheduleClassCompliance("S2", "Dr Smith"));

        Assert.Null(ex);
    }

    // ================================================================
    // S3 — Pharmacist Only: requires ApprovedBy
    // ================================================================

    [Fact]
    public void ValidateScheduleClassCompliance_S3_NoApproval_Throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            ProductManager.ValidateScheduleClassCompliance("S3", null));

        Assert.Contains("pharmacist approval", ex.Message);
        Assert.Contains("ApprovedBy", ex.Message);
    }

    [Fact]
    public void ValidateScheduleClassCompliance_S3_EmptyApproval_Throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            ProductManager.ValidateScheduleClassCompliance("S3", ""));

        Assert.Contains("pharmacist approval", ex.Message);
    }

    [Fact]
    public void ValidateScheduleClassCompliance_S3_WhitespaceApproval_Throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            ProductManager.ValidateScheduleClassCompliance("S3", "   "));

        Assert.Contains("pharmacist approval", ex.Message);
    }

    [Fact]
    public void ValidateScheduleClassCompliance_S3_WithApproval_DoesNotThrow()
    {
        var ex = Record.Exception(() =>
            ProductManager.ValidateScheduleClassCompliance("S3", "Pharmacist Jane Doe"));

        Assert.Null(ex);
    }

    // ================================================================
    // S4 — Prescription Only: always blocked
    // ================================================================

    [Fact]
    public void ValidateScheduleClassCompliance_S4_Blocked_NoApproval()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            ProductManager.ValidateScheduleClassCompliance("S4", null));

        Assert.Contains("Prescription Only", ex.Message);
        Assert.Contains("dispensary system", ex.Message);
    }

    [Fact]
    public void ValidateScheduleClassCompliance_S4_Blocked_EvenWithApproval()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            ProductManager.ValidateScheduleClassCompliance("S4", "Dr Smith"));

        Assert.Contains("Prescription Only", ex.Message);
    }

    // ================================================================
    // Unscheduled — no restrictions
    // ================================================================

    [Fact]
    public void ValidateScheduleClassCompliance_Unscheduled_NoApproval_DoesNotThrow()
    {
        var ex = Record.Exception(() =>
            ProductManager.ValidateScheduleClassCompliance("Unscheduled", null));

        Assert.Null(ex);
    }

    [Fact]
    public void ValidateScheduleClassCompliance_Unscheduled_WithApproval_DoesNotThrow()
    {
        var ex = Record.Exception(() =>
            ProductManager.ValidateScheduleClassCompliance("Unscheduled", "Anyone"));

        Assert.Null(ex);
    }

    // ================================================================
    // DTO Tests — ApprovedBy field
    // ================================================================

    [Fact]
    public void RecordStockMovementRequest_ApprovedBy_DefaultNull()
    {
        var request = new RecordStockMovementRequest();

        Assert.Null(request.ApprovedBy);
    }

    [Fact]
    public void RecordStockMovementRequest_ApprovedBy_CanBeSet()
    {
        var request = new RecordStockMovementRequest { ApprovedBy = "Pharmacist Smith" };

        Assert.Equal("Pharmacist Smith", request.ApprovedBy);
    }

    [Fact]
    public void StockMovementDto_ApprovedBy_Included()
    {
        var dto = new StockMovementDto(
            Guid.NewGuid(), Guid.NewGuid(), "stock_out", 5,
            "REF-001", "S3 sale", DateTimeOffset.UtcNow, "cashier", "Pharmacist Jane");

        Assert.Equal("Pharmacist Jane", dto.ApprovedBy);
    }

    [Fact]
    public void StockMovementDto_ApprovedBy_NullForNonS3()
    {
        var dto = new StockMovementDto(
            Guid.NewGuid(), Guid.NewGuid(), "stock_in", 10,
            null, null, DateTimeOffset.UtcNow, "admin", null);

        Assert.Null(dto.ApprovedBy);
    }
}
