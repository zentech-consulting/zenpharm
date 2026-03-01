namespace Api.Features.Knowledge;

public interface IKnowledgeManager
{
    Task<KnowledgeEntryDto> CreateAsync(CreateKnowledgeEntryRequest request, CancellationToken ct = default);
    Task<KnowledgeEntryDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<KnowledgeListResponse> ListAsync(int page, int pageSize, string? category, CancellationToken ct = default);
    Task<KnowledgeEntryDto?> UpdateAsync(Guid id, UpdateKnowledgeEntryRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<KnowledgeSearchResult>> SearchAsync(KnowledgeSearchRequest request, CancellationToken ct = default);
}
