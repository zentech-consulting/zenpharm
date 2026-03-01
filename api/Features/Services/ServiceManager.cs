using Api.Common;
using Dapper;

namespace Api.Features.Services;

internal sealed class ServiceManager(
    ITenantDb db,
    ILogger<ServiceManager> logger) : IServiceManager
{
    public async Task<ServiceDto> CreateAsync(CreateServiceRequest request, CancellationToken ct = default)
    {
        using var conn = await db.CreateAsync();

        var sql = """
            INSERT INTO dbo.Services (Name, Description, Category, Price, DurationMinutes, IsActive)
            OUTPUT INSERTED.Id, INSERTED.Name, INSERTED.Description, INSERTED.Category,
                   INSERTED.Price, INSERTED.DurationMinutes, INSERTED.IsActive, INSERTED.CreatedAt
            VALUES (@Name, @Description, @Category, @Price, @DurationMinutes, @IsActive)
            """;

        logger.LogInformation("Creating service: {Name}", request.Name);

        return await conn.QuerySingleAsync<ServiceDto>(
            new CommandDefinition(sql, request, cancellationToken: ct));
    }

    public async Task<ServiceDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        using var conn = await db.CreateAsync();

        var sql = """
            SELECT Id, Name, Description, Category, Price, DurationMinutes, IsActive, CreatedAt
            FROM dbo.Services
            WHERE Id = @Id
            """;

        return await conn.QuerySingleOrDefaultAsync<ServiceDto>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }

    public async Task<ServiceListResponse> ListAsync(int page, int pageSize, string? category, CancellationToken ct = default)
    {
        using var conn = await db.CreateAsync();

        var whereClause = string.IsNullOrWhiteSpace(category)
            ? ""
            : "WHERE Category = @Category";

        var countSql = $"SELECT COUNT(*) FROM dbo.Services {whereClause}";

        var listSql = $"""
            SELECT Id, Name, Description, Category, Price, DurationMinutes, IsActive, CreatedAt
            FROM dbo.Services
            {whereClause}
            ORDER BY CreatedAt DESC
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

        var items = await conn.QueryAsync<ServiceDto>(
            new CommandDefinition(listSql, parameters, cancellationToken: ct));

        return new ServiceListResponse(items.ToList(), totalCount);
    }

    public async Task<ServiceDto?> UpdateAsync(Guid id, UpdateServiceRequest request, CancellationToken ct = default)
    {
        using var conn = await db.CreateAsync();

        var sql = """
            UPDATE dbo.Services
            SET Name = @Name, Description = @Description, Category = @Category,
                Price = @Price, DurationMinutes = @DurationMinutes, IsActive = @IsActive,
                UpdatedAt = SYSUTCDATETIME()
            OUTPUT INSERTED.Id, INSERTED.Name, INSERTED.Description, INSERTED.Category,
                   INSERTED.Price, INSERTED.DurationMinutes, INSERTED.IsActive, INSERTED.CreatedAt
            WHERE Id = @Id
            """;

        logger.LogInformation("Updating service {Id}", id);

        return await conn.QuerySingleOrDefaultAsync<ServiceDto>(
            new CommandDefinition(sql, new
            {
                Id = id,
                request.Name,
                request.Description,
                request.Category,
                request.Price,
                request.DurationMinutes,
                request.IsActive
            }, cancellationToken: ct));
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        using var conn = await db.CreateAsync();

        var sql = "DELETE FROM dbo.Services WHERE Id = @Id";

        logger.LogInformation("Deleting service {Id}", id);

        var rows = await conn.ExecuteAsync(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));

        return rows > 0;
    }
}
