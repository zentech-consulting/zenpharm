using Api.Features.Platform;
using Xunit;

namespace Api.Tests.Platform;

public class PbsSyncManagerTests
{
    [Theory]
    [InlineData("Paracetamol 500mg", "2622B")]
    [InlineData("Ibuprofen 200mg", "3146E")]
    [InlineData("Amoxicillin", "1003K")]
    [InlineData("Metformin", "2164J")]
    [InlineData("Atorvastatin", "8575B")]
    [InlineData("Salbutamol", "2464J")]
    [InlineData("Sertraline", "8826T")]
    public void FindPbsCode_MatchesKnownIngredients(string activeIngredients, string expectedCode)
    {
        var code = PbsCodeMapping.FindPbsCode(activeIngredients);
        Assert.Equal(expectedCode, code);
    }

    [Theory]
    [InlineData("Paracetamol 500mg, Codeine phosphate 8mg", "2622B")]  // matches first ingredient
    [InlineData("Amoxicillin + Clavulanic acid", "8035Y")]  // exact match for combination
    public void FindPbsCode_MatchesFirstIngredient(string activeIngredients, string expectedCode)
    {
        var code = PbsCodeMapping.FindPbsCode(activeIngredients);
        Assert.Equal(expectedCode, code);
    }

    [Fact]
    public void FindPbsCode_CaseInsensitive()
    {
        Assert.Equal("2622B", PbsCodeMapping.FindPbsCode("PARACETAMOL"));
        Assert.Equal("3146E", PbsCodeMapping.FindPbsCode("ibuprofen"));
        Assert.Equal("1003K", PbsCodeMapping.FindPbsCode("Amoxicillin"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Menthol 10%")]
    [InlineData("Camphor 11%")]
    public void FindPbsCode_NoMatch_ReturnsNull(string? activeIngredients)
    {
        Assert.Null(PbsCodeMapping.FindPbsCode(activeIngredients));
    }

    [Fact]
    public void PbsCodeMapping_HasAtLeast79Entries()
    {
        Assert.True(PbsCodeMapping.ByActiveIngredient.Count >= 79,
            $"Expected >= 79 PBS entries, got {PbsCodeMapping.ByActiveIngredient.Count}");
    }

    [Fact]
    public void PbsCodeMapping_AllCodesNonEmpty()
    {
        foreach (var (ingredient, code) in PbsCodeMapping.ByActiveIngredient)
        {
            Assert.False(string.IsNullOrWhiteSpace(code),
                $"PBS code for '{ingredient}' is empty.");
        }
    }

    [Fact]
    public void PbsSyncResult_RecordValues()
    {
        var result = new PbsSyncResult(500, 120, 80);
        Assert.Equal(500, result.TotalProducts);
        Assert.Equal(120, result.Matched);
        Assert.Equal(80, result.Updated);
    }

    [Fact]
    public void PbsSummary_RecordValues()
    {
        var summary = new PbsSummary(500, 120, 380);
        Assert.Equal(500, summary.TotalProducts);
        Assert.Equal(120, summary.WithPbsCode);
        Assert.Equal(380, summary.WithoutPbsCode);
    }
}
