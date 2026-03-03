using Api.Common;
using Dapper;

namespace Api.Features.Shop;

internal sealed class ShopManager(
    ITenantDb db,
    ICatalogDb catalogDb,
    ILogger<ShopManager> logger) : IShopManager
{
    internal static string MapStockAvailability(int stockQuantity) =>
        stockQuantity switch
        {
            <= 0 => "Out of Stock",
            <= 10 => "Low Stock",
            _ => "In Stock"
        };

    public async Task<ShopProductListResponse> ListProductsAsync(
        string? category, string? search, bool? featured,
        int page, int pageSize, CancellationToken ct = default)
    {
        using var conn = await db.CreateAsync();

        var conditions = new List<string>
        {
            "IsVisible = 1",
            "ScheduleClass != 'S4'"
        };

        if (!string.IsNullOrWhiteSpace(category))
            conditions.Add("Category = @Category");
        if (!string.IsNullOrWhiteSpace(search))
            conditions.Add("(COALESCE(CustomName, MasterProductName) LIKE @Search OR GenericName LIKE @Search OR Brand LIKE @Search)");
        if (featured == true)
            conditions.Add("IsFeatured = 1");

        var whereClause = "WHERE " + string.Join(" AND ", conditions);

        var countSql = $"SELECT COUNT(*) FROM dbo.TenantProducts {whereClause}";

        var listSql = $"""
            SELECT Id, COALESCE(CustomName, MasterProductName) AS Name,
                   GenericName, Brand, Category, ScheduleClass,
                   COALESCE(CustomPrice, DefaultPrice) AS Price,
                   ImageUrl, StockQuantity, IsFeatured
            FROM dbo.TenantProducts
            {whereClause}
            ORDER BY IsFeatured DESC, SortOrder, COALESCE(CustomName, MasterProductName)
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """;

        var parameters = new
        {
            Category = category,
            Search = string.IsNullOrWhiteSpace(search)
                ? null
                : $"%{search!.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]")}%",
            Offset = (page - 1) * pageSize,
            PageSize = pageSize
        };

        var totalCount = await conn.ExecuteScalarAsync<int>(
            new CommandDefinition(countSql, parameters, cancellationToken: ct));

        var rows = await conn.QueryAsync<dynamic>(
            new CommandDefinition(listSql, parameters, cancellationToken: ct));

        var items = rows.Select(r => new ShopProductDto(
            (Guid)r.Id,
            (string)r.Name,
            (string?)r.GenericName,
            (string?)r.Brand,
            (string)r.Category,
            (string)r.ScheduleClass,
            (decimal)r.Price,
            (string?)r.ImageUrl,
            MapStockAvailability((int)r.StockQuantity),
            (bool)r.IsFeatured
        )).ToList();

        logger.LogInformation("Shop: listed {Count} products (page {Page})", items.Count, page);

        return new ShopProductListResponse(items, totalCount);
    }

    public async Task<ShopProductDetailDto?> GetProductAsync(Guid id, CancellationToken ct = default)
    {
        using var conn = await db.CreateAsync();

        var tenantSql = """
            SELECT Id,
                   COALESCE(CustomName, MasterProductName) AS Name,
                   GenericName, Brand, Category, ScheduleClass,
                   COALESCE(CustomPrice, DefaultPrice) AS Price,
                   ImageUrl, StockQuantity, IsFeatured, MasterProductId
            FROM dbo.TenantProducts
            WHERE Id = @Id AND IsVisible = 1 AND ScheduleClass != 'S4'
            """;

        var row = await conn.QuerySingleOrDefaultAsync<dynamic>(
            new CommandDefinition(tenantSql, new { Id = id }, cancellationToken: ct));

        if (row is null) return null;

        // Fetch extended details from catalogue DB
        string? activeIngredients = null;
        string? warnings = null;
        string? description = null;

        try
        {
            using var catalogConn = await catalogDb.CreateAsync();
            var catalogSql = "SELECT ActiveIngredients, Warnings, Description FROM dbo.MasterProducts WHERE Id = @Id";
            var master = await catalogConn.QuerySingleOrDefaultAsync<dynamic>(
                new CommandDefinition(catalogSql, new { Id = (Guid)row.MasterProductId }, cancellationToken: ct));

            if (master is not null)
            {
                activeIngredients = (string?)master.ActiveIngredients;
                warnings = (string?)master.Warnings;
                description = (string?)master.Description;
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch catalogue details for product {Id}", id);
        }

        return new ShopProductDetailDto(
            (Guid)row.Id,
            (string)row.Name,
            (string?)row.GenericName,
            (string?)row.Brand,
            (string)row.Category,
            (string)row.ScheduleClass,
            (decimal)row.Price,
            (string?)row.ImageUrl,
            MapStockAvailability((int)row.StockQuantity),
            (bool)row.IsFeatured,
            activeIngredients,
            warnings,
            description
        );
    }

    public async Task<IReadOnlyList<string>> ListCategoriesAsync(CancellationToken ct = default)
    {
        using var conn = await db.CreateAsync();

        var sql = """
            SELECT DISTINCT Category
            FROM dbo.TenantProducts
            WHERE IsVisible = 1 AND ScheduleClass != 'S4'
            ORDER BY Category
            """;

        var categories = await conn.QueryAsync<string>(
            new CommandDefinition(sql, cancellationToken: ct));

        return categories.ToList();
    }
}
