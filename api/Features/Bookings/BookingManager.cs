using Api.Common;
using Dapper;

namespace Api.Features.Bookings;

internal sealed class BookingManager(
    ITenantDb db,
    ILogger<BookingManager> logger) : IBookingManager
{
    public async Task<BookingDto> CreateAsync(CreateBookingRequest request, CancellationToken ct = default)
    {
        using var conn = await db.CreateAsync();

        // Look up service duration to compute EndTime
        var service = await conn.QuerySingleOrDefaultAsync<(int DurationMinutes, string Name)>(
            new CommandDefinition(
                "SELECT DurationMinutes, Name FROM dbo.Services WHERE Id = @ServiceId",
                new { request.ServiceId },
                cancellationToken: ct));

        if (service == default)
            throw new InvalidOperationException($"Service {request.ServiceId} not found");

        var endTime = request.StartTime.AddMinutes(service.DurationMinutes);

        logger.LogInformation("Creating booking for client {ClientId}, service {ServiceName}",
            request.ClientId, service.Name);

        var insertSql = """
            DECLARE @InsertedId TABLE (Id UNIQUEIDENTIFIER);

            INSERT INTO dbo.Bookings (ClientId, ServiceId, EmployeeId, StartTime, EndTime, Notes)
            OUTPUT INSERTED.Id INTO @InsertedId
            VALUES (@ClientId, @ServiceId, @EmployeeId, @StartTime, @EndTime, @Notes);

            SELECT b.Id, b.ClientId, c.FirstName + ' ' + c.LastName AS ClientName,
                   b.ServiceId, s.Name AS ServiceName,
                   b.EmployeeId, e.FirstName + ' ' + e.LastName AS EmployeeName,
                   b.StartTime, b.EndTime, b.Status, b.Notes, b.CreatedAt
            FROM dbo.Bookings b
            INNER JOIN dbo.Clients c ON c.Id = b.ClientId
            INNER JOIN dbo.Services s ON s.Id = b.ServiceId
            LEFT JOIN dbo.Employees e ON e.Id = b.EmployeeId
            WHERE b.Id = (SELECT TOP 1 Id FROM @InsertedId)
            """;

        return await conn.QuerySingleAsync<BookingDto>(
            new CommandDefinition(insertSql, new
            {
                request.ClientId,
                request.ServiceId,
                request.EmployeeId,
                request.StartTime,
                EndTime = endTime,
                request.Notes
            }, cancellationToken: ct));
    }

    public async Task<BookingDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        using var conn = await db.CreateAsync();

        var sql = """
            SELECT b.Id, b.ClientId, c.FirstName + ' ' + c.LastName AS ClientName,
                   b.ServiceId, s.Name AS ServiceName,
                   b.EmployeeId, e.FirstName + ' ' + e.LastName AS EmployeeName,
                   b.StartTime, b.EndTime, b.Status, b.Notes, b.CreatedAt
            FROM dbo.Bookings b
            INNER JOIN dbo.Clients c ON c.Id = b.ClientId
            INNER JOIN dbo.Services s ON s.Id = b.ServiceId
            LEFT JOIN dbo.Employees e ON e.Id = b.EmployeeId
            WHERE b.Id = @Id
            """;

        return await conn.QuerySingleOrDefaultAsync<BookingDto>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }

    public async Task<BookingListResponse> ListAsync(int page, int pageSize, DateOnly? date, Guid? employeeId, CancellationToken ct = default)
    {
        using var conn = await db.CreateAsync();

        var conditions = new List<string>();
        if (date.HasValue)
            conditions.Add("CAST(b.StartTime AS DATE) = @Date");
        if (employeeId.HasValue)
            conditions.Add("b.EmployeeId = @EmployeeId");

        var whereClause = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : "";

        var countSql = $"SELECT COUNT(*) FROM dbo.Bookings b {whereClause}";

        var listSql = $"""
            SELECT b.Id, b.ClientId, c.FirstName + ' ' + c.LastName AS ClientName,
                   b.ServiceId, s.Name AS ServiceName,
                   b.EmployeeId, e.FirstName + ' ' + e.LastName AS EmployeeName,
                   b.StartTime, b.EndTime, b.Status, b.Notes, b.CreatedAt
            FROM dbo.Bookings b
            INNER JOIN dbo.Clients c ON c.Id = b.ClientId
            INNER JOIN dbo.Services s ON s.Id = b.ServiceId
            LEFT JOIN dbo.Employees e ON e.Id = b.EmployeeId
            {whereClause}
            ORDER BY b.StartTime DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """;

        var parameters = new
        {
            Date = date,
            EmployeeId = employeeId,
            Offset = (page - 1) * pageSize,
            PageSize = pageSize
        };

        var totalCount = await conn.ExecuteScalarAsync<int>(
            new CommandDefinition(countSql, parameters, cancellationToken: ct));

        var items = await conn.QueryAsync<BookingDto>(
            new CommandDefinition(listSql, parameters, cancellationToken: ct));

        return new BookingListResponse(items.ToList(), totalCount);
    }

    public async Task<BookingDto?> UpdateAsync(Guid id, UpdateBookingRequest request, CancellationToken ct = default)
    {
        using var conn = await db.CreateAsync();

        var sql = """
            UPDATE dbo.Bookings
            SET EmployeeId = @EmployeeId, StartTime = @StartTime,
                Status = @Status, Notes = @Notes, UpdatedAt = SYSUTCDATETIME()
            WHERE Id = @Id;

            SELECT b.Id, b.ClientId, c.FirstName + ' ' + c.LastName AS ClientName,
                   b.ServiceId, s.Name AS ServiceName,
                   b.EmployeeId, e.FirstName + ' ' + e.LastName AS EmployeeName,
                   b.StartTime, b.EndTime, b.Status, b.Notes, b.CreatedAt
            FROM dbo.Bookings b
            INNER JOIN dbo.Clients c ON c.Id = b.ClientId
            INNER JOIN dbo.Services s ON s.Id = b.ServiceId
            LEFT JOIN dbo.Employees e ON e.Id = b.EmployeeId
            WHERE b.Id = @Id
            """;

        logger.LogInformation("Updating booking {Id}", id);

        return await conn.QuerySingleOrDefaultAsync<BookingDto>(
            new CommandDefinition(sql, new
            {
                Id = id,
                request.EmployeeId,
                request.StartTime,
                request.Status,
                request.Notes
            }, cancellationToken: ct));
    }

    public async Task<bool> CancelAsync(Guid id, CancellationToken ct = default)
    {
        using var conn = await db.CreateAsync();

        var sql = """
            UPDATE dbo.Bookings
            SET Status = 'cancelled', UpdatedAt = SYSUTCDATETIME()
            WHERE Id = @Id AND Status NOT IN ('cancelled', 'completed')
            """;

        logger.LogInformation("Cancelling booking {Id}", id);

        var rows = await conn.ExecuteAsync(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));

        return rows > 0;
    }

    public async Task<IReadOnlyList<AvailableSlotDto>> GetAvailableSlotsAsync(
        Guid serviceId, DateOnly date, Guid? employeeId, CancellationToken ct = default)
    {
        using var conn = await db.CreateAsync();

        var service = await conn.QuerySingleOrDefaultAsync<(int DurationMinutes, string Name)>(
            new CommandDefinition(
                "SELECT DurationMinutes, Name FROM dbo.Services WHERE Id = @ServiceId",
                new { ServiceId = serviceId },
                cancellationToken: ct));

        if (service == default)
            return [];

        // Fetch existing bookings for this date (not cancelled)
        var existingConditions = "CAST(b.StartTime AS DATE) = @Date AND b.Status != 'cancelled'";
        if (employeeId.HasValue)
            existingConditions += " AND b.EmployeeId = @EmployeeId";

        var existingBookings = await conn.QueryAsync<(DateTimeOffset StartTime, DateTimeOffset EndTime)>(
            new CommandDefinition(
                $"SELECT StartTime, EndTime FROM dbo.Bookings b WHERE {existingConditions}",
                new { Date = date, EmployeeId = employeeId },
                cancellationToken: ct));

        var booked = existingBookings.ToList();
        var slots = new List<AvailableSlotDto>();
        var dayStart = new DateTimeOffset(date.Year, date.Month, date.Day, 9, 0, 0, TimeSpan.Zero);
        var dayEnd = new DateTimeOffset(date.Year, date.Month, date.Day, 17, 0, 0, TimeSpan.Zero);

        for (var slotStart = dayStart; slotStart.AddMinutes(service.DurationMinutes) <= dayEnd; slotStart = slotStart.AddMinutes(service.DurationMinutes))
        {
            var slotEnd = slotStart.AddMinutes(service.DurationMinutes);
            var overlaps = booked.Any(b => b.StartTime < slotEnd && b.EndTime > slotStart);
            if (!overlaps)
                slots.Add(new AvailableSlotDto(slotStart, slotEnd));
        }

        return slots;
    }
}
