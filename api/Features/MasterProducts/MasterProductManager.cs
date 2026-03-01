using Api.Common;
using Dapper;

namespace Api.Features.MasterProducts;

internal sealed class MasterProductManager(
    ICatalogDb catalogDb,
    ILogger<MasterProductManager> logger) : IMasterProductManager
{
    private const string SelectColumns = """
        Id, Sku, Name, Category, Description, UnitPrice, Unit,
        GenericName, Brand, Barcode, ScheduleClass, PackSize,
        ActiveIngredients, Warnings, PbsItemCode, ImageUrl,
        IsActive, CreatedAt
        """;

    public async Task<MasterProductDto> CreateAsync(CreateMasterProductRequest request, CancellationToken ct = default)
    {
        using var conn = await catalogDb.CreateAsync();

        var sql = $"""
            INSERT INTO dbo.MasterProducts
                (Sku, Name, Category, Description, UnitPrice, Unit,
                 GenericName, Brand, Barcode, ScheduleClass, PackSize,
                 ActiveIngredients, Warnings, PbsItemCode, ImageUrl)
            OUTPUT INSERTED.{SelectColumns.Replace("\n", "").Replace("        ", "")}
            VALUES
                (@Sku, @Name, @Category, @Description, @UnitPrice, @Unit,
                 @GenericName, @Brand, @Barcode, @ScheduleClass, @PackSize,
                 @ActiveIngredients, @Warnings, @PbsItemCode, @ImageUrl)
            """;

        logger.LogInformation("Creating master product: {Sku} — {Name}", request.Sku, request.Name);

        return await conn.QuerySingleAsync<MasterProductDto>(
            new CommandDefinition(sql, request, cancellationToken: ct));
    }

    public async Task<MasterProductDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        using var conn = await catalogDb.CreateAsync();

        var sql = $"""
            SELECT {SelectColumns}
            FROM dbo.MasterProducts
            WHERE Id = @Id
            """;

        return await conn.QuerySingleOrDefaultAsync<MasterProductDto>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }

    public async Task<MasterProductListResponse> ListAsync(
        int page, int pageSize, string? category, string? search, string? scheduleClass, CancellationToken ct = default)
    {
        using var conn = await catalogDb.CreateAsync();

        var conditions = new List<string> { "IsActive = 1" };

        if (!string.IsNullOrWhiteSpace(category))
            conditions.Add("Category = @Category");
        if (!string.IsNullOrWhiteSpace(search))
            conditions.Add("(Name LIKE @Search OR GenericName LIKE @Search OR Brand LIKE @Search OR Barcode LIKE @Search)");
        if (!string.IsNullOrWhiteSpace(scheduleClass))
            conditions.Add("ScheduleClass = @ScheduleClass");

        var whereClause = "WHERE " + string.Join(" AND ", conditions);

        var countSql = $"SELECT COUNT(*) FROM dbo.MasterProducts {whereClause}";

        var listSql = $"""
            SELECT {SelectColumns}
            FROM dbo.MasterProducts
            {whereClause}
            ORDER BY Name
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """;

        var parameters = new
        {
            Category = category,
            Search = string.IsNullOrWhiteSpace(search) ? null : $"%{search}%",
            ScheduleClass = scheduleClass,
            Offset = (page - 1) * pageSize,
            PageSize = pageSize
        };

        var totalCount = await conn.ExecuteScalarAsync<int>(
            new CommandDefinition(countSql, parameters, cancellationToken: ct));

        var items = await conn.QueryAsync<MasterProductDto>(
            new CommandDefinition(listSql, parameters, cancellationToken: ct));

        return new MasterProductListResponse(items.ToList(), totalCount);
    }

    public async Task<MasterProductDto?> UpdateAsync(Guid id, UpdateMasterProductRequest request, CancellationToken ct = default)
    {
        using var conn = await catalogDb.CreateAsync();

        var sql = $"""
            UPDATE dbo.MasterProducts
            SET Name = @Name, Category = @Category, Description = @Description,
                UnitPrice = @UnitPrice, Unit = @Unit,
                GenericName = @GenericName, Brand = @Brand, Barcode = @Barcode,
                ScheduleClass = @ScheduleClass, PackSize = @PackSize,
                ActiveIngredients = @ActiveIngredients, Warnings = @Warnings,
                PbsItemCode = @PbsItemCode, ImageUrl = @ImageUrl,
                IsActive = @IsActive, UpdatedAt = SYSUTCDATETIME()
            OUTPUT INSERTED.{SelectColumns.Replace("\n", "").Replace("        ", "")}
            WHERE Id = @Id
            """;

        logger.LogInformation("Updating master product {Id}", id);

        return await conn.QuerySingleOrDefaultAsync<MasterProductDto>(
            new CommandDefinition(sql, new
            {
                Id = id,
                request.Name,
                request.Category,
                request.Description,
                request.UnitPrice,
                request.Unit,
                request.GenericName,
                request.Brand,
                request.Barcode,
                request.ScheduleClass,
                request.PackSize,
                request.ActiveIngredients,
                request.Warnings,
                request.PbsItemCode,
                request.ImageUrl,
                request.IsActive
            }, cancellationToken: ct));
    }
}
