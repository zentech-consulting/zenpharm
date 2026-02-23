namespace Api.Features.Knowledge;

internal sealed class KnowledgeManager(
    ILogger<KnowledgeManager> logger) : IKnowledgeManager
{
    public Task<KnowledgeEntryDto> CreateAsync(CreateKnowledgeEntryRequest request, CancellationToken ct = default)
    {
        throw new NotImplementedException("Knowledge module not yet implemented — see Phase 1, Subtask 9");
    }

    public Task<KnowledgeEntryDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        throw new NotImplementedException("Knowledge module not yet implemented — see Phase 1, Subtask 9");
    }

    public Task<KnowledgeListResponse> ListAsync(int page, int pageSize, string? category, CancellationToken ct = default)
    {
        throw new NotImplementedException("Knowledge module not yet implemented — see Phase 1, Subtask 9");
    }

    public Task<KnowledgeEntryDto?> UpdateAsync(Guid id, UpdateKnowledgeEntryRequest request, CancellationToken ct = default)
    {
        throw new NotImplementedException("Knowledge module not yet implemented — see Phase 1, Subtask 9");
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        throw new NotImplementedException("Knowledge module not yet implemented — see Phase 1, Subtask 9");
    }

    public Task<IReadOnlyList<KnowledgeSearchResult>> SearchAsync(KnowledgeSearchRequest request, CancellationToken ct = default)
    {
        throw new NotImplementedException("Knowledge module not yet implemented — see Phase 1, Subtask 9");
    }
}
