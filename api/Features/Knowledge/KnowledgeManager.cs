using Api.Common;
using Dapper;

namespace Api.Features.Knowledge;

internal sealed class KnowledgeManager(
    ITenantDb db,
    ILogger<KnowledgeManager> logger) : IKnowledgeManager
{
    public async Task<KnowledgeEntryDto> CreateAsync(CreateKnowledgeEntryRequest request, CancellationToken ct = default)
    {
        using var conn = await db.CreateAsync();

        var tags = request.Tags is { Count: > 0 } ? string.Join(",", request.Tags) : null;

        var sql = """
            INSERT INTO dbo.KnowledgeEntries (Title, Content, Category, Tags)
            OUTPUT INSERTED.Id, INSERTED.Title, INSERTED.Content, INSERTED.Category,
                   INSERTED.Tags, INSERTED.CreatedAt, INSERTED.UpdatedAt
            VALUES (@Title, @Content, @Category, @Tags)
            """;

        logger.LogInformation("Creating knowledge entry: {Title}", request.Title);

        var row = await conn.QuerySingleAsync<KnowledgeEntryRow>(
            new CommandDefinition(sql, new
            {
                request.Title,
                request.Content,
                request.Category,
                Tags = tags
            }, cancellationToken: ct));

        return row.ToDto();
    }

    public async Task<KnowledgeEntryDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        using var conn = await db.CreateAsync();

        var sql = "SELECT Id, Title, Content, Category, Tags, CreatedAt, UpdatedAt FROM dbo.KnowledgeEntries WHERE Id = @Id";

        var row = await conn.QuerySingleOrDefaultAsync<KnowledgeEntryRow>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));

        return row?.ToDto();
    }

    public async Task<KnowledgeListResponse> ListAsync(int page, int pageSize, string? category, CancellationToken ct = default)
    {
        using var conn = await db.CreateAsync();

        var whereClause = string.IsNullOrWhiteSpace(category)
            ? ""
            : "WHERE Category = @Category";

        var countSql = $"SELECT COUNT(*) FROM dbo.KnowledgeEntries {whereClause}";

        var listSql = $"""
            SELECT Id, Title, Content, Category, Tags, CreatedAt, UpdatedAt
            FROM dbo.KnowledgeEntries
            {whereClause}
            ORDER BY UpdatedAt DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """;

        var parameters = new
        {
            Category = category,
            Offset = (page - 1) * pageSize,
            PageSize = pageSize
        };

        var totalCount = await conn.ExecuteScalarAsync<int>(
            new CommandDefinition(countSql, parameters, cancellationToken: ct));

        var rows = await conn.QueryAsync<KnowledgeEntryRow>(
            new CommandDefinition(listSql, parameters, cancellationToken: ct));

        return new KnowledgeListResponse(rows.Select(r => r.ToDto()).ToList(), totalCount);
    }

    public async Task<KnowledgeEntryDto?> UpdateAsync(Guid id, UpdateKnowledgeEntryRequest request, CancellationToken ct = default)
    {
        using var conn = await db.CreateAsync();

        var tags = request.Tags is { Count: > 0 } ? string.Join(",", request.Tags) : null;

        var sql = """
            UPDATE dbo.KnowledgeEntries
            SET Title = @Title, Content = @Content, Category = @Category,
                Tags = @Tags, UpdatedAt = SYSUTCDATETIME()
            OUTPUT INSERTED.Id, INSERTED.Title, INSERTED.Content, INSERTED.Category,
                   INSERTED.Tags, INSERTED.CreatedAt, INSERTED.UpdatedAt
            WHERE Id = @Id
            """;

        logger.LogInformation("Updating knowledge entry {Id}", id);

        var row = await conn.QuerySingleOrDefaultAsync<KnowledgeEntryRow>(
            new CommandDefinition(sql, new
            {
                Id = id,
                request.Title,
                request.Content,
                request.Category,
                Tags = tags
            }, cancellationToken: ct));

        return row?.ToDto();
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        using var conn = await db.CreateAsync();

        var rows = await conn.ExecuteAsync(
            new CommandDefinition(
                "DELETE FROM dbo.KnowledgeEntries WHERE Id = @Id",
                new { Id = id }, cancellationToken: ct));

        return rows > 0;
    }

    public async Task<IReadOnlyList<KnowledgeSearchResult>> SearchAsync(KnowledgeSearchRequest request, CancellationToken ct = default)
    {
        using var conn = await db.CreateAsync();

        var sql = """
            SELECT Id, Title, Content, Category, Tags, CreatedAt, UpdatedAt
            FROM dbo.KnowledgeEntries
            WHERE Title LIKE @Query OR Content LIKE @Query
            """;

        var rows = await conn.QueryAsync<KnowledgeEntryRow>(
            new CommandDefinition(sql, new { Query = $"%{request.Query}%" }, cancellationToken: ct));

        var results = rows.Select(r =>
        {
            var score = r.Title.Contains(request.Query, StringComparison.OrdinalIgnoreCase) ? 1.0 : 0.5;
            return new KnowledgeSearchResult(r.ToDto(), score);
        })
        .OrderByDescending(r => r.Score)
        .Take(request.MaxResults)
        .ToList();

        logger.LogInformation("Knowledge search for '{Query}' returned {Count} results",
            request.Query, results.Count);

        return results;
    }

    internal sealed class KnowledgeEntryRow
    {
        public Guid Id { get; init; }
        public string Title { get; init; } = "";
        public string Content { get; init; } = "";
        public string Category { get; init; } = "";
        public string? Tags { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset UpdatedAt { get; init; }

        public KnowledgeEntryDto ToDto() => new(
            Id, Title, Content, Category,
            string.IsNullOrWhiteSpace(Tags) ? [] : Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList(),
            CreatedAt, UpdatedAt);
    }
}
