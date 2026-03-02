using Api.Common;
using Dapper;

namespace Api.Features.Products;

internal sealed class ProductManager(
    ITenantDb db,
    ICatalogDb catalogDb,
    ILogger<ProductManager> logger) : IProductManager
{
    private const string SelectColumns = """
        Id, MasterProductId, MasterProductName, GenericName, Brand,
        Category, ScheduleClass, DefaultPrice, CustomName, CustomPrice,
        ImageUrl, StockQuantity, ReorderLevel, ExpiryDate,
        IsVisible, IsFeatured, SortOrder, CreatedAt
        """;

    public async Task<ProductDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        using var conn = await db.CreateAsync();

        var sql = $"""
            SELECT {SelectColumns}
            FROM dbo.TenantProducts
            WHERE Id = @Id
            """;

        return await conn.QuerySingleOrDefaultAsync<ProductDto>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }

    public async Task<ProductListResponse> ListAsync(
        int page, int pageSize, string? search, bool lowStockOnly, bool expiringOnly, CancellationToken ct = default)
    {
        using var conn = await db.CreateAsync();

        var conditions = new List<string>();

        if (!string.IsNullOrWhiteSpace(search))
            conditions.Add("(MasterProductName LIKE @Search OR GenericName LIKE @Search OR Brand LIKE @Search OR CustomName LIKE @Search)");
        if (lowStockOnly)
            conditions.Add("StockQuantity <= ReorderLevel");
        if (expiringOnly)
            conditions.Add("ExpiryDate IS NOT NULL AND ExpiryDate <= DATEADD(DAY, 30, GETUTCDATE())");

        var whereClause = conditions.Count > 0
            ? "WHERE " + string.Join(" AND ", conditions)
            : "";

        var countSql = $"SELECT COUNT(*) FROM dbo.TenantProducts {whereClause}";

        var listSql = $"""
            SELECT {SelectColumns}
            FROM dbo.TenantProducts
            {whereClause}
            ORDER BY SortOrder, MasterProductName
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """;

        var parameters = new
        {
            Search = string.IsNullOrWhiteSpace(search) ? null : $"%{search}%",
            Offset = (page - 1) * pageSize,
            PageSize = pageSize
        };

        var totalCount = await conn.ExecuteScalarAsync<int>(
            new CommandDefinition(countSql, parameters, cancellationToken: ct));

        var items = await conn.QueryAsync<ProductDto>(
            new CommandDefinition(listSql, parameters, cancellationToken: ct));

        return new ProductListResponse(items.ToList(), totalCount);
    }

    public async Task<IReadOnlyList<ProductDto>> ImportFromCatalogueAsync(Guid[] masterProductIds, CancellationToken ct = default)
    {
        if (masterProductIds.Length == 0) return [];

        using var catalogConn = await catalogDb.CreateAsync();

        var masterSql = """
            SELECT Id, Name, GenericName, Brand, Category, ScheduleClass, UnitPrice, ImageUrl
            FROM dbo.MasterProducts
            WHERE Id IN @Ids AND IsActive = 1
            """;

        var masterProducts = (await catalogConn.QueryAsync<dynamic>(
            new CommandDefinition(masterSql, new { Ids = masterProductIds }, cancellationToken: ct))).ToList();

        if (masterProducts.Count == 0) return [];

        using var tenantConn = await db.CreateAsync();

        var imported = new List<ProductDto>();

        foreach (var mp in masterProducts)
        {
            var insertSql = $"""
                IF NOT EXISTS (SELECT 1 FROM dbo.TenantProducts WHERE MasterProductId = @MasterProductId)
                BEGIN
                    DECLARE @InsertedId TABLE (Id UNIQUEIDENTIFIER);

                    INSERT INTO dbo.TenantProducts
                        (MasterProductId, MasterProductName, GenericName, Brand, Category, ScheduleClass, DefaultPrice, ImageUrl)
                    OUTPUT INSERTED.Id INTO @InsertedId
                    VALUES
                        (@MasterProductId, @MasterProductName, @GenericName, @Brand, @Category, @ScheduleClass, @DefaultPrice, @ImageUrl);

                    SELECT {SelectColumns}
                    FROM dbo.TenantProducts
                    WHERE Id = (SELECT TOP 1 Id FROM @InsertedId);
                END
                """;

            var row = await tenantConn.QuerySingleOrDefaultAsync<ProductDto>(
                new CommandDefinition(insertSql, new
                {
                    MasterProductId = (Guid)mp.Id,
                    MasterProductName = (string)mp.Name,
                    GenericName = (string?)mp.GenericName,
                    Brand = (string?)mp.Brand,
                    Category = (string)mp.Category,
                    ScheduleClass = (string)mp.ScheduleClass,
                    DefaultPrice = (decimal)mp.UnitPrice,
                    ImageUrl = (string?)mp.ImageUrl
                }, cancellationToken: ct));

            if (row is not null)
                imported.Add(row);
        }

        logger.LogInformation("Imported {Count} products from catalogue", imported.Count);
        return imported;
    }

    public async Task<ProductDto?> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken ct = default)
    {
        using var conn = await db.CreateAsync();

        var sql = $"""
            UPDATE dbo.TenantProducts
            SET CustomName = @CustomName, CustomPrice = @CustomPrice,
                ReorderLevel = @ReorderLevel, ExpiryDate = @ExpiryDate,
                IsVisible = @IsVisible, IsFeatured = @IsFeatured,
                SortOrder = @SortOrder, UpdatedAt = SYSUTCDATETIME()
            WHERE Id = @Id;

            SELECT {SelectColumns}
            FROM dbo.TenantProducts
            WHERE Id = @Id;
            """;

        logger.LogInformation("Updating tenant product {Id}", id);

        return await conn.QuerySingleOrDefaultAsync<ProductDto>(
            new CommandDefinition(sql, new
            {
                Id = id,
                request.CustomName,
                request.CustomPrice,
                request.ReorderLevel,
                request.ExpiryDate,
                request.IsVisible,
                request.IsFeatured,
                request.SortOrder
            }, cancellationToken: ct));
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        using var conn = await db.CreateAsync();

        logger.LogInformation("Deleting tenant product {Id}", id);

        var rows = await conn.ExecuteAsync(
            new CommandDefinition("DELETE FROM dbo.TenantProducts WHERE Id = @Id",
                new { Id = id }, cancellationToken: ct));

        return rows > 0;
    }

    public async Task<StockMovementDto> RecordStockMovementAsync(
        Guid productId, RecordStockMovementRequest request, CancellationToken ct = default)
    {
        using var conn = await db.CreateAsync();

        // Fetch product to check schedule class compliance
        var product = await conn.QuerySingleOrDefaultAsync<ProductDto>(
            new CommandDefinition(
                $"SELECT {SelectColumns} FROM dbo.TenantProducts WHERE Id = @Id",
                new { Id = productId }, cancellationToken: ct));

        if (product is null)
            throw new InvalidOperationException($"Product {productId} not found");

        // Schedule class compliance for stock_out movements
        if (request.MovementType == "stock_out")
        {
            ValidateScheduleClassCompliance(product.ScheduleClass, request.ApprovedBy);
        }

        using var tx = conn.BeginTransaction();

        var quantityDelta = request.MovementType switch
        {
            "stock_in" or "return" => Math.Abs(request.Quantity),
            "stock_out" or "expired" => -Math.Abs(request.Quantity),
            "adjustment" => request.Quantity,
            _ => request.Quantity
        };

        var updateSql = """
            UPDATE dbo.TenantProducts
            SET StockQuantity = StockQuantity + @Delta, UpdatedAt = SYSUTCDATETIME()
            WHERE Id = @ProductId
            """;

        await conn.ExecuteAsync(
            new CommandDefinition(updateSql, new { Delta = quantityDelta, ProductId = productId },
                transaction: tx, cancellationToken: ct));

        var insertSql = """
            INSERT INTO dbo.StockMovements (TenantProductId, MovementType, Quantity, Reference, Notes, CreatedBy, ApprovedBy)
            OUTPUT INSERTED.Id, INSERTED.TenantProductId, INSERTED.MovementType, INSERTED.Quantity,
                   INSERTED.Reference, INSERTED.Notes, INSERTED.CreatedAt, INSERTED.CreatedBy, INSERTED.ApprovedBy
            VALUES (@TenantProductId, @MovementType, @Quantity, @Reference, @Notes, @CreatedBy, @ApprovedBy)
            """;

        var movement = await conn.QuerySingleAsync<StockMovementDto>(
            new CommandDefinition(insertSql, new
            {
                TenantProductId = productId,
                request.MovementType,
                request.Quantity,
                request.Reference,
                request.Notes,
                request.CreatedBy,
                request.ApprovedBy
            }, transaction: tx, cancellationToken: ct));

        tx.Commit();

        logger.LogInformation("Stock movement recorded: {Type} {Quantity} for product {ProductId} Schedule={Schedule}",
            request.MovementType, request.Quantity, productId, product.ScheduleClass);

        return movement;
    }

    /// <summary>
    /// Validates TGA schedule class rules for stock_out movements:
    /// - S2 (Pharmacy Medicine): no special approval needed
    /// - S3 (Pharmacist Only): requires ApprovedBy (pharmacist name)
    /// - S4 (Prescription Only): blocked for OTC sale via admin panel
    /// </summary>
    internal static void ValidateScheduleClassCompliance(string scheduleClass, string? approvedBy)
    {
        switch (scheduleClass)
        {
            case "S4":
                throw new InvalidOperationException(
                    "S4 (Prescription Only) products cannot be dispensed via the admin panel. " +
                    "Prescription dispensing must be handled through the dispensary system.");

            case "S3" when string.IsNullOrWhiteSpace(approvedBy):
                throw new InvalidOperationException(
                    "S3 (Pharmacist Only) products require pharmacist approval. " +
                    "Please provide the approving pharmacist's name in the ApprovedBy field.");
        }
    }

    public async Task<StockMovementListResponse> ListStockMovementsAsync(
        Guid productId, int page, int pageSize, CancellationToken ct = default)
    {
        using var conn = await db.CreateAsync();

        var countSql = "SELECT COUNT(*) FROM dbo.StockMovements WHERE TenantProductId = @ProductId";

        var listSql = """
            SELECT Id, TenantProductId, MovementType, Quantity, Reference, Notes, CreatedAt, CreatedBy, ApprovedBy
            FROM dbo.StockMovements
            WHERE TenantProductId = @ProductId
            ORDER BY CreatedAt DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """;

        var parameters = new { ProductId = productId, Offset = (page - 1) * pageSize, PageSize = pageSize };

        var totalCount = await conn.ExecuteScalarAsync<int>(
            new CommandDefinition(countSql, parameters, cancellationToken: ct));

        var items = await conn.QueryAsync<StockMovementDto>(
            new CommandDefinition(listSql, parameters, cancellationToken: ct));

        return new StockMovementListResponse(items.ToList(), totalCount);
    }

    public async Task<LowStockSummary> GetLowStockSummaryAsync(CancellationToken ct = default)
    {
        using var conn = await db.CreateAsync();

        var totalProducts = await conn.ExecuteScalarAsync<int>(
            new CommandDefinition("SELECT COUNT(*) FROM dbo.TenantProducts", cancellationToken: ct));

        var lowStockSql = $"""
            SELECT {SelectColumns}
            FROM dbo.TenantProducts
            WHERE StockQuantity <= ReorderLevel
            ORDER BY StockQuantity ASC
            """;

        var lowStockItems = await conn.QueryAsync<ProductDto>(
            new CommandDefinition(lowStockSql, cancellationToken: ct));

        var items = lowStockItems.ToList();
        return new LowStockSummary(totalProducts, items.Count, items);
    }

    public async Task<ExpiryAlertResponse> GetExpiryAlertsAsync(int daysAhead, CancellationToken ct = default)
    {
        using var conn = await db.CreateAsync();

        var sql = $"""
            SELECT {SelectColumns}
            FROM dbo.TenantProducts
            WHERE ExpiryDate IS NOT NULL AND ExpiryDate <= DATEADD(DAY, @DaysAhead, GETUTCDATE())
            ORDER BY ExpiryDate ASC
            """;

        var items = (await conn.QueryAsync<ProductDto>(
            new CommandDefinition(sql, new { DaysAhead = daysAhead }, cancellationToken: ct))).ToList();

        return new ExpiryAlertResponse(items.Count, items);
    }
}
