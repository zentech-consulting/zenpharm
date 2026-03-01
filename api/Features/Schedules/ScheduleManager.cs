using Api.Common;
using Dapper;

namespace Api.Features.Schedules;

internal sealed class ScheduleManager(
    ITenantDb db,
    ILogger<ScheduleManager> logger) : IScheduleManager
{
    public async Task<ScheduleDto> CreateAsync(CreateScheduleRequest request, CancellationToken ct = default)
    {
        using var conn = await db.CreateAsync();

        var sql = """
            DECLARE @InsertedId TABLE (Id UNIQUEIDENTIFIER);

            INSERT INTO dbo.Schedules (EmployeeId, Date, StartTime, EndTime, Location, Notes)
            OUTPUT INSERTED.Id INTO @InsertedId
            VALUES (@EmployeeId, @Date, @StartTime, @EndTime, @Location, @Notes);

            SELECT sc.Id, sc.EmployeeId, e.FirstName + ' ' + e.LastName AS EmployeeName,
                   sc.Date, sc.StartTime, sc.EndTime, sc.Location, sc.Notes, sc.CreatedAt
            FROM dbo.Schedules sc
            INNER JOIN dbo.Employees e ON e.Id = sc.EmployeeId
            WHERE sc.Id = (SELECT TOP 1 Id FROM @InsertedId)
            """;

        logger.LogInformation("Creating schedule for employee {EmployeeId} on {Date}",
            request.EmployeeId, request.Date);

        return await conn.QuerySingleAsync<ScheduleDto>(
            new CommandDefinition(sql, new
            {
                request.EmployeeId,
                request.Date,
                StartTime = request.StartTime.ToTimeSpan(),
                EndTime = request.EndTime.ToTimeSpan(),
                request.Location,
                request.Notes
            }, cancellationToken: ct));
    }

    public async Task<ScheduleDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        using var conn = await db.CreateAsync();

        var sql = """
            SELECT sc.Id, sc.EmployeeId, e.FirstName + ' ' + e.LastName AS EmployeeName,
                   sc.Date, sc.StartTime, sc.EndTime, sc.Location, sc.Notes, sc.CreatedAt
            FROM dbo.Schedules sc
            INNER JOIN dbo.Employees e ON e.Id = sc.EmployeeId
            WHERE sc.Id = @Id
            """;

        return await conn.QuerySingleOrDefaultAsync<ScheduleDto>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }

    public async Task<ScheduleListResponse> ListAsync(DateOnly? startDate, DateOnly? endDate, Guid? employeeId, CancellationToken ct = default)
    {
        using var conn = await db.CreateAsync();

        var conditions = new List<string>();
        if (startDate.HasValue)
            conditions.Add("sc.Date >= @StartDate");
        if (endDate.HasValue)
            conditions.Add("sc.Date <= @EndDate");
        if (employeeId.HasValue)
            conditions.Add("sc.EmployeeId = @EmployeeId");

        var whereClause = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : "";

        var countSql = $"SELECT COUNT(*) FROM dbo.Schedules sc {whereClause}";

        var listSql = $"""
            SELECT sc.Id, sc.EmployeeId, e.FirstName + ' ' + e.LastName AS EmployeeName,
                   sc.Date, sc.StartTime, sc.EndTime, sc.Location, sc.Notes, sc.CreatedAt
            FROM dbo.Schedules sc
            INNER JOIN dbo.Employees e ON e.Id = sc.EmployeeId
            {whereClause}
            ORDER BY sc.Date, sc.StartTime
            """;

        var parameters = new
        {
            StartDate = startDate,
            EndDate = endDate,
            EmployeeId = employeeId
        };

        var totalCount = await conn.ExecuteScalarAsync<int>(
            new CommandDefinition(countSql, parameters, cancellationToken: ct));

        var items = await conn.QueryAsync<ScheduleDto>(
            new CommandDefinition(listSql, parameters, cancellationToken: ct));

        return new ScheduleListResponse(items.ToList(), totalCount);
    }

    public async Task<ScheduleDto?> UpdateAsync(Guid id, UpdateScheduleRequest request, CancellationToken ct = default)
    {
        using var conn = await db.CreateAsync();

        var sql = """
            UPDATE dbo.Schedules
            SET StartTime = @StartTime, EndTime = @EndTime,
                Location = @Location, Notes = @Notes,
                UpdatedAt = SYSUTCDATETIME()
            WHERE Id = @Id;

            SELECT sc.Id, sc.EmployeeId, e.FirstName + ' ' + e.LastName AS EmployeeName,
                   sc.Date, sc.StartTime, sc.EndTime, sc.Location, sc.Notes, sc.CreatedAt
            FROM dbo.Schedules sc
            INNER JOIN dbo.Employees e ON e.Id = sc.EmployeeId
            WHERE sc.Id = @Id
            """;

        logger.LogInformation("Updating schedule {Id}", id);

        return await conn.QuerySingleOrDefaultAsync<ScheduleDto>(
            new CommandDefinition(sql, new
            {
                Id = id,
                StartTime = request.StartTime.ToTimeSpan(),
                EndTime = request.EndTime.ToTimeSpan(),
                request.Location,
                request.Notes
            }, cancellationToken: ct));
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        using var conn = await db.CreateAsync();

        var sql = "DELETE FROM dbo.Schedules WHERE Id = @Id";

        logger.LogInformation("Deleting schedule {Id}", id);

        var rows = await conn.ExecuteAsync(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));

        return rows > 0;
    }

    public async Task<IReadOnlyList<ScheduleDto>> GenerateAsync(GenerateScheduleRequest request, CancellationToken ct = default)
    {
        using var conn = await db.CreateAsync();

        // Get target employees
        var employeeSql = request.EmployeeIds is { Count: > 0 }
            ? "SELECT Id, FirstName, LastName FROM dbo.Employees WHERE IsActive = 1 AND Id IN @EmployeeIds"
            : "SELECT Id, FirstName, LastName FROM dbo.Employees WHERE IsActive = 1";

        var employees = (await conn.QueryAsync<(Guid Id, string FirstName, string LastName)>(
            new CommandDefinition(employeeSql, new { EmployeeIds = request.EmployeeIds }, cancellationToken: ct)))
            .ToList();

        if (employees.Count == 0)
            return [];

        // Generate Mon-Fri 09:00-17:00 entries, skipping existing
        var generated = new List<ScheduleDto>();
        var defaultStart = new TimeOnly(9, 0);
        var defaultEnd = new TimeOnly(17, 0);

        for (var d = request.StartDate; d <= request.EndDate; d = d.AddDays(1))
        {
            var dow = d.DayOfWeek;
            if (dow == DayOfWeek.Saturday || dow == DayOfWeek.Sunday)
                continue;

            foreach (var emp in employees)
            {
                // Check if schedule already exists for this employee/date
                var exists = await conn.ExecuteScalarAsync<int>(
                    new CommandDefinition(
                        "SELECT COUNT(*) FROM dbo.Schedules WHERE EmployeeId = @EmpId AND Date = @Date",
                        new { EmpId = emp.Id, Date = d },
                        cancellationToken: ct));

                if (exists > 0) continue;

                var insertSql = """
                    DECLARE @InsertedId TABLE (Id UNIQUEIDENTIFIER);

                    INSERT INTO dbo.Schedules (EmployeeId, Date, StartTime, EndTime)
                    OUTPUT INSERTED.Id INTO @InsertedId
                    VALUES (@EmployeeId, @Date, @StartTime, @EndTime);

                    SELECT TOP 1 Id FROM @InsertedId
                    """;

                var newId = await conn.QuerySingleAsync<Guid>(
                    new CommandDefinition(insertSql, new
                    {
                        EmployeeId = emp.Id,
                        Date = d,
                        StartTime = defaultStart.ToTimeSpan(),
                        EndTime = defaultEnd.ToTimeSpan()
                    }, cancellationToken: ct));

                generated.Add(new ScheduleDto(
                    newId, emp.Id, $"{emp.FirstName} {emp.LastName}",
                    d, defaultStart, defaultEnd, null, null, DateTimeOffset.UtcNow));
            }
        }

        logger.LogInformation("Generated {Count} schedule entries for {StartDate} to {EndDate}",
            generated.Count, request.StartDate, request.EndDate);

        return generated;
    }
}
