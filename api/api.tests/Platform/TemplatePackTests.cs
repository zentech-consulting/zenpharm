using Api.Features.Platform;
using Xunit;

namespace Api.Tests.Platform;

public class TemplatePackTests
{
    [Fact]
    public void GetAll_Returns3Packs()
    {
        var packs = TemplatePacks.GetAll();
        Assert.Equal(3, packs.Count);
    }

    [Fact]
    public void GetDefault_ReturnsCommunityEssentials()
    {
        var pack = TemplatePacks.GetDefault();
        Assert.Equal("community-essentials", pack.Id);
        Assert.Empty(pack.Categories); // empty = all categories
    }

    [Fact]
    public void GetById_ReturnsCorrectPack()
    {
        var pack = TemplatePacks.GetById("health-wellness");
        Assert.NotNull(pack);
        Assert.Equal("Health & Wellness Focus", pack.Name);
        Assert.NotEmpty(pack.Categories);
    }

    [Fact]
    public void GetById_CaseInsensitive()
    {
        var pack = TemplatePacks.GetById("QUICK-START");
        Assert.NotNull(pack);
        Assert.Equal("quick-start", pack.Id);
    }

    [Fact]
    public void GetById_UnknownId_ReturnsNull()
    {
        var pack = TemplatePacks.GetById("nonexistent");
        Assert.Null(pack);
    }

    [Fact]
    public void IncludesCategory_DefaultPackIncludesEverything()
    {
        var pack = TemplatePacks.GetDefault();
        Assert.True(TemplatePacks.IncludesCategory(pack, "Pain Relief"));
        Assert.True(TemplatePacks.IncludesCategory(pack, "Antibiotics"));
        Assert.True(TemplatePacks.IncludesCategory(pack, "Random Category"));
    }

    [Fact]
    public void IncludesCategory_QuickStartFiltersCorrectly()
    {
        var pack = TemplatePacks.GetById("quick-start")!;
        Assert.True(TemplatePacks.IncludesCategory(pack, "Pain Relief"));
        Assert.True(TemplatePacks.IncludesCategory(pack, "Cold & Flu"));
        Assert.False(TemplatePacks.IncludesCategory(pack, "Antibiotics"));
        Assert.False(TemplatePacks.IncludesCategory(pack, "Mental Health"));
    }

    [Fact]
    public void IncludesCategory_HealthWellnessFiltersCorrectly()
    {
        var pack = TemplatePacks.GetById("health-wellness")!;
        Assert.True(TemplatePacks.IncludesCategory(pack, "Vitamins & Supplements"));
        Assert.True(TemplatePacks.IncludesCategory(pack, "Skin Care"));
        Assert.False(TemplatePacks.IncludesCategory(pack, "Antibiotics"));
        Assert.False(TemplatePacks.IncludesCategory(pack, "Cardiovascular"));
    }
}
