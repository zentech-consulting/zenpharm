using Api.Features.Knowledge;
using Xunit;

namespace Api.Tests.Knowledge;

public class KnowledgeContractTests
{
    [Fact]
    public void CreateKnowledgeEntryRequest_DefaultValues()
    {
        var req = new CreateKnowledgeEntryRequest();

        Assert.Equal("", req.Title);
        Assert.Equal("", req.Content);
        Assert.Equal("general", req.Category);
        Assert.Null(req.Tags);
    }

    [Fact]
    public void UpdateKnowledgeEntryRequest_DefaultValues()
    {
        var req = new UpdateKnowledgeEntryRequest();

        Assert.Equal("", req.Title);
        Assert.Equal("", req.Content);
        Assert.Equal("general", req.Category);
        Assert.Null(req.Tags);
    }

    [Fact]
    public void KnowledgeEntryDto_RecordEquality()
    {
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var tags = new List<string> { "faq", "pricing" };

        var a = new KnowledgeEntryDto(id, "Pricing FAQ", "Our prices are...", "faq", tags, now, now);
        var b = new KnowledgeEntryDto(id, "Pricing FAQ", "Our prices are...", "faq", tags, now, now);

        Assert.Equal(a, b);
    }

    [Fact]
    public void KnowledgeSearchRequest_DefaultValues()
    {
        var req = new KnowledgeSearchRequest();

        Assert.Equal("", req.Query);
        Assert.Equal(5, req.MaxResults);
    }

    [Fact]
    public void KnowledgeSearchResult_ScoreProperty()
    {
        var now = DateTimeOffset.UtcNow;
        var entry = new KnowledgeEntryDto(Guid.NewGuid(), "Title", "Content", "general", [], now, now);
        var result = new KnowledgeSearchResult(entry, 0.85);

        Assert.Equal(0.85, result.Score);
        Assert.Equal("Title", result.Entry.Title);
    }

    [Fact]
    public void KnowledgeListResponse_EmptyItems()
    {
        var response = new KnowledgeListResponse([], 0);

        Assert.Empty(response.Items);
        Assert.Equal(0, response.TotalCount);
    }
}
