using Api.Common;
using Api.Features.Notifications;
using Dapper;

namespace Api.Features.Orders;

internal sealed class OrderManager(
    ITenantDb db,
    IConfiguration configuration,
    ILogger<OrderManager> logger) : IOrderManager
{
    private const decimal GstRate = 0.10m;

    public async Task<OrderDto> CreateGuestOrderAsync(
        CreateGuestOrderRequest request, CancellationToken ct = default)
    {
        if (request.Items.Length == 0)
            throw new InvalidOperationException("Order must contain at least one item.");

        using var conn = await db.CreateAsync();

        // 1. Validate products: visible, not S4, sufficient stock
        var productIds = request.Items.Select(i => i.ProductId).Distinct().ToArray();

        var productSql = """
            SELECT Id, COALESCE(CustomName, MasterProductName) AS Name,
                   ScheduleClass, COALESCE(CustomPrice, DefaultPrice) AS Price,
                   StockQuantity, IsVisible
            FROM dbo.TenantProducts
            WHERE Id IN @Ids
            """;

        var products = (await conn.QueryAsync<dynamic>(
            new CommandDefinition(productSql, new { Ids = productIds }, cancellationToken: ct)))
            .ToDictionary(p => (Guid)p.Id);

        // Validate all items
        var errors = new List<string>();
        foreach (var item in request.Items)
        {
            if (!products.TryGetValue(item.ProductId, out var product))
            {
                errors.Add($"Product {item.ProductId} not found.");
                continue;
            }
            if (!(bool)product.IsVisible)
                errors.Add($"Product '{product.Name}' is not available.");
            if ((string)product.ScheduleClass == "S4")
                errors.Add($"Product '{product.Name}' is prescription-only (S4) and cannot be ordered online.");
            if ((int)product.StockQuantity < item.Quantity)
                errors.Add($"Insufficient stock for '{product.Name}': available {product.StockQuantity}, requested {item.Quantity}.");
        }

        if (errors.Count > 0)
            throw new InvalidOperationException(string.Join(" ", errors));

        // 2. Find or create client
        using var tx = conn.BeginTransaction();

        var findClientSql = """
            SELECT TOP 1 Id FROM dbo.Clients
            WHERE Email = @Email AND Phone = @Phone
            """;

        var clientId = await conn.QuerySingleOrDefaultAsync<Guid?>(
            new CommandDefinition(findClientSql, new { request.Email, request.Phone },
                transaction: tx, cancellationToken: ct));

        if (clientId is null)
        {
            var createClientSql = """
                DECLARE @NewClientId TABLE(Id UNIQUEIDENTIFIER);
                INSERT INTO dbo.Clients (FirstName, LastName, Email, Phone)
                OUTPUT INSERTED.Id INTO @NewClientId
                VALUES (@FirstName, @LastName, @Email, @Phone);
                SELECT Id FROM @NewClientId;
                """;

            clientId = await conn.QuerySingleAsync<Guid>(
                new CommandDefinition(createClientSql, new
                {
                    request.FirstName, request.LastName, request.Email, request.Phone
                }, transaction: tx, cancellationToken: ct));

            logger.LogInformation("Created new client {ClientId} for guest order", clientId);
        }

        // 3. Generate order number
        var orderNumber = await GenerateOrderNumberAsync(conn, tx, ct);

        // 4. Calculate totals
        decimal subtotal = 0;
        foreach (var item in request.Items)
        {
            var product = products[item.ProductId];
            subtotal += (decimal)product.Price * item.Quantity;
        }
        var taxAmount = Math.Round(subtotal * GstRate, 2);
        var total = subtotal + taxAmount;

        // 5. Calculate estimated ready time
        var estimatedReady = CalculateEstimatedReadyTime();

        // 6. Insert order
        var insertOrderSql = """
            DECLARE @OrderId TABLE(Id UNIQUEIDENTIFIER);
            INSERT INTO dbo.Orders (OrderNumber, ClientId, Status, Subtotal, TaxAmount, Total, Notes, EstimatedReadyAt)
            OUTPUT INSERTED.Id INTO @OrderId
            VALUES (@OrderNumber, @ClientId, 'pending', @Subtotal, @TaxAmount, @Total, @Notes, @EstimatedReadyAt);
            SELECT Id FROM @OrderId;
            """;

        var orderId = await conn.QuerySingleAsync<Guid>(
            new CommandDefinition(insertOrderSql, new
            {
                OrderNumber = orderNumber,
                ClientId = clientId.Value,
                Subtotal = subtotal,
                TaxAmount = taxAmount,
                Total = total,
                request.Notes,
                EstimatedReadyAt = estimatedReady
            }, transaction: tx, cancellationToken: ct));

        // 7. Insert order items + deduct stock
        foreach (var item in request.Items)
        {
            var product = products[item.ProductId];
            var itemSubtotal = (decimal)product.Price * item.Quantity;

            var insertItemSql = """
                INSERT INTO dbo.OrderItems (OrderId, TenantProductId, ProductName, Quantity, UnitPrice, Subtotal)
                VALUES (@OrderId, @TenantProductId, @ProductName, @Quantity, @UnitPrice, @Subtotal);
                """;

            await conn.ExecuteAsync(
                new CommandDefinition(insertItemSql, new
                {
                    OrderId = orderId,
                    TenantProductId = item.ProductId,
                    ProductName = (string)product.Name,
                    item.Quantity,
                    UnitPrice = (decimal)product.Price,
                    Subtotal = itemSubtotal
                }, transaction: tx, cancellationToken: ct));

            // Deduct stock
            var deductSql = """
                UPDATE dbo.TenantProducts
                SET StockQuantity = StockQuantity - @Qty, UpdatedAt = SYSUTCDATETIME()
                WHERE Id = @ProductId;
                """;

            await conn.ExecuteAsync(
                new CommandDefinition(deductSql, new { Qty = item.Quantity, ProductId = item.ProductId },
                    transaction: tx, cancellationToken: ct));

            // Record stock movement
            var movementSql = """
                INSERT INTO dbo.StockMovements (TenantProductId, MovementType, Quantity, Reference, Notes, CreatedBy)
                VALUES (@TenantProductId, 'stock_out', @Quantity, @Reference, 'Online shop order', 'System');
                """;

            await conn.ExecuteAsync(
                new CommandDefinition(movementSql, new
                {
                    TenantProductId = item.ProductId,
                    item.Quantity,
                    Reference = orderNumber
                }, transaction: tx, cancellationToken: ct));
        }

        tx.Commit();

        logger.LogInformation("Order {OrderNumber} created: {Total:C} ({ItemCount} items)",
            orderNumber, total, request.Items.Length);

        return (await GetByIdCoreAsync(conn, orderId, ct))!;
    }

    public async Task<OrderDto?> GetByOrderNumberAsync(string orderNumber, CancellationToken ct = default)
    {
        using var conn = await db.CreateAsync();

        var orderId = await conn.QuerySingleOrDefaultAsync<Guid?>(
            new CommandDefinition("SELECT Id FROM dbo.Orders WHERE OrderNumber = @OrderNumber",
                new { OrderNumber = orderNumber }, cancellationToken: ct));

        if (orderId is null) return null;

        return await GetByIdCoreAsync(conn, orderId.Value, ct);
    }

    public async Task<OrderDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        using var conn = await db.CreateAsync();
        return await GetByIdCoreAsync(conn, id, ct);
    }

    public async Task<OrderListResponse> ListAsync(
        int page, int pageSize, string? status, string? search, CancellationToken ct = default)
    {
        using var conn = await db.CreateAsync();

        var conditions = new List<string>();
        if (!string.IsNullOrWhiteSpace(status))
            conditions.Add("o.Status = @Status");
        if (!string.IsNullOrWhiteSpace(search))
            conditions.Add("(o.OrderNumber LIKE @Search OR c.FirstName LIKE @Search OR c.LastName LIKE @Search)");

        var whereClause = conditions.Count > 0
            ? "WHERE " + string.Join(" AND ", conditions)
            : "";

        var countSql = $"""
            SELECT COUNT(*)
            FROM dbo.Orders o
            JOIN dbo.Clients c ON c.Id = o.ClientId
            {whereClause}
            """;

        var listSql = $"""
            SELECT o.Id, o.OrderNumber, c.FirstName + ' ' + c.LastName AS ClientName,
                   o.Status, o.Total, o.EstimatedReadyAt, o.CreatedAt,
                   (SELECT COUNT(*) FROM dbo.OrderItems oi WHERE oi.OrderId = o.Id) AS ItemCount
            FROM dbo.Orders o
            JOIN dbo.Clients c ON c.Id = o.ClientId
            {whereClause}
            ORDER BY o.CreatedAt DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """;

        var parameters = new
        {
            Status = status,
            Search = string.IsNullOrWhiteSpace(search) ? null : $"%{search}%",
            Offset = (page - 1) * pageSize,
            PageSize = pageSize
        };

        var totalCount = await conn.ExecuteScalarAsync<int>(
            new CommandDefinition(countSql, parameters, cancellationToken: ct));

        var items = (await conn.QueryAsync<OrderSummaryDto>(
            new CommandDefinition(listSql, parameters, cancellationToken: ct))).ToList();

        return new OrderListResponse(items, totalCount);
    }

    public async Task<OrderDto?> MarkAsReadyAsync(Guid id, CancellationToken ct = default)
    {
        using var conn = await db.CreateAsync();

        var sql = """
            UPDATE dbo.Orders
            SET Status = 'ready', ReadyNotifiedAt = SYSUTCDATETIME(), UpdatedAt = SYSUTCDATETIME()
            WHERE Id = @Id AND Status = 'pending';
            """;

        var rows = await conn.ExecuteAsync(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));

        if (rows == 0) return null;

        // Send SMS notification
        var order = await GetByIdCoreAsync(conn, id, ct);
        if (order is not null)
        {
            var clientPhoneSql = "SELECT Phone FROM dbo.Clients WHERE Id = @ClientId";
            var phone = await conn.QuerySingleOrDefaultAsync<string?>(
                new CommandDefinition(clientPhoneSql, new { order.ClientId }, cancellationToken: ct));

            if (!string.IsNullOrWhiteSpace(phone))
            {
                var clientNameParts = order.ClientName.Split(' ');
                var firstName = clientNameParts.Length > 0 ? clientNameParts[0] : "Customer";
                var message = $"Hi {firstName}, your order {order.OrderNumber} is ready for collection. " +
                              "Please visit us at your convenience.";

                await Sms.SendAsync(configuration, logger, phone, message, ct);
                logger.LogInformation("Order {OrderNumber} marked ready, SMS sent to client", order.OrderNumber);
            }
        }

        return order;
    }

    public async Task<OrderDto?> MarkAsCollectedAsync(Guid id, CancellationToken ct = default)
    {
        using var conn = await db.CreateAsync();

        var sql = """
            UPDATE dbo.Orders
            SET Status = 'collected', CollectedAt = SYSUTCDATETIME(), UpdatedAt = SYSUTCDATETIME()
            WHERE Id = @Id AND Status = 'ready';
            """;

        var rows = await conn.ExecuteAsync(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));

        if (rows == 0) return null;

        logger.LogInformation("Order {Id} marked as collected", id);
        return await GetByIdCoreAsync(conn, id, ct);
    }

    public async Task<OrderDto?> CancelOrderAsync(Guid id, string reason, CancellationToken ct = default)
    {
        using var conn = await db.CreateAsync();

        // Only cancel pending or ready orders
        var checkSql = "SELECT Status FROM dbo.Orders WHERE Id = @Id";
        var currentStatus = await conn.QuerySingleOrDefaultAsync<string?>(
            new CommandDefinition(checkSql, new { Id = id }, cancellationToken: ct));

        if (currentStatus is null) return null;
        if (currentStatus is "collected" or "cancelled")
            throw new InvalidOperationException($"Cannot cancel an order with status '{currentStatus}'.");

        using var tx = conn.BeginTransaction();

        var cancelSql = """
            UPDATE dbo.Orders
            SET Status = 'cancelled', CancelledAt = SYSUTCDATETIME(),
                CancellationReason = @Reason, UpdatedAt = SYSUTCDATETIME()
            WHERE Id = @Id;
            """;

        await conn.ExecuteAsync(
            new CommandDefinition(cancelSql, new { Id = id, Reason = reason },
                transaction: tx, cancellationToken: ct));

        // Return stock for each item
        var itemsSql = "SELECT TenantProductId, Quantity FROM dbo.OrderItems WHERE OrderId = @OrderId";
        var items = await conn.QueryAsync<(Guid TenantProductId, int Quantity)>(
            new CommandDefinition(itemsSql, new { OrderId = id }, transaction: tx, cancellationToken: ct));

        var orderNumberSql = "SELECT OrderNumber FROM dbo.Orders WHERE Id = @Id";
        var orderNumber = await conn.QuerySingleAsync<string>(
            new CommandDefinition(orderNumberSql, new { Id = id }, transaction: tx, cancellationToken: ct));

        foreach (var item in items)
        {
            var returnStockSql = """
                UPDATE dbo.TenantProducts
                SET StockQuantity = StockQuantity + @Qty, UpdatedAt = SYSUTCDATETIME()
                WHERE Id = @ProductId;
                """;

            await conn.ExecuteAsync(
                new CommandDefinition(returnStockSql, new { Qty = item.Quantity, ProductId = item.TenantProductId },
                    transaction: tx, cancellationToken: ct));

            var movementSql = """
                INSERT INTO dbo.StockMovements (TenantProductId, MovementType, Quantity, Reference, Notes, CreatedBy)
                VALUES (@TenantProductId, 'return', @Quantity, @Reference, @Notes, 'System');
                """;

            await conn.ExecuteAsync(
                new CommandDefinition(movementSql, new
                {
                    item.TenantProductId,
                    item.Quantity,
                    Reference = orderNumber,
                    Notes = $"Order cancelled: {reason}"
                }, transaction: tx, cancellationToken: ct));
        }

        tx.Commit();

        logger.LogInformation("Order {OrderNumber} cancelled: {Reason}", orderNumber, reason);
        return await GetByIdCoreAsync(conn, id, ct);
    }

    // ─── Helpers ──────────────────────────────────────────────────────

    private static async Task<OrderDto?> GetByIdCoreAsync(
        System.Data.IDbConnection conn, Guid id, CancellationToken ct)
    {
        var orderSql = """
            SELECT o.Id, o.OrderNumber, o.ClientId,
                   c.FirstName + ' ' + c.LastName AS ClientName,
                   o.Status, o.Subtotal, o.TaxAmount, o.Total, o.Notes,
                   o.EstimatedReadyAt, o.ReadyNotifiedAt, o.CollectedAt,
                   o.CancelledAt, o.CancellationReason, o.CreatedAt
            FROM dbo.Orders o
            JOIN dbo.Clients c ON c.Id = o.ClientId
            WHERE o.Id = @Id
            """;

        var order = await conn.QuerySingleOrDefaultAsync<dynamic>(
            new CommandDefinition(orderSql, new { Id = id }, cancellationToken: ct));

        if (order is null) return null;

        var itemsSql = """
            SELECT Id, TenantProductId, ProductName, Quantity, UnitPrice, Subtotal
            FROM dbo.OrderItems
            WHERE OrderId = @OrderId
            ORDER BY ProductName
            """;

        var items = (await conn.QueryAsync<OrderItemDto>(
            new CommandDefinition(itemsSql, new { OrderId = id }, cancellationToken: ct))).ToList();

        return new OrderDto(
            (Guid)order.Id,
            (string)order.OrderNumber,
            (Guid)order.ClientId,
            (string)order.ClientName,
            (string)order.Status,
            (decimal)order.Subtotal,
            (decimal)order.TaxAmount,
            (decimal)order.Total,
            (string?)order.Notes,
            (DateTimeOffset?)order.EstimatedReadyAt,
            (DateTimeOffset?)order.ReadyNotifiedAt,
            (DateTimeOffset?)order.CollectedAt,
            (DateTimeOffset?)order.CancelledAt,
            (string?)order.CancellationReason,
            (DateTimeOffset)order.CreatedAt,
            items
        );
    }

    private static async Task<string> GenerateOrderNumberAsync(
        System.Data.IDbConnection conn, System.Data.IDbTransaction tx, CancellationToken ct)
    {
        var today = DateTime.UtcNow.ToString("yyyyMMdd");
        var prefix = $"ORD-{today}-";

        var sql = """
            SELECT ISNULL(MAX(CAST(RIGHT(OrderNumber, 4) AS INT)), 0) + 1
            FROM dbo.Orders
            WHERE OrderNumber LIKE @Prefix + '%'
            """;

        var seq = await conn.QuerySingleAsync<int>(
            new CommandDefinition(sql, new { Prefix = prefix }, transaction: tx, cancellationToken: ct));

        return $"{prefix}{seq:D4}";
    }

    internal static DateTimeOffset CalculateEstimatedReadyTime()
    {
        // AEST = UTC+10
        var nowAest = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(10));
        var cutoff = new TimeOnly(15, 0); // 3:00 PM AEST

        if (nowAest.TimeOfDay < cutoff.ToTimeSpan())
        {
            // Same day, approximately 2 hours from now
            return nowAest.AddHours(2).ToUniversalTime();
        }

        // Next business day at 10:00 AM AEST
        var nextDay = nowAest.Date.AddDays(1);
        while (nextDay.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            nextDay = nextDay.AddDays(1);

        return new DateTimeOffset(nextDay.AddHours(10), TimeSpan.FromHours(10)).ToUniversalTime();
    }
}
