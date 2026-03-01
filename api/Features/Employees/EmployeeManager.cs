using Api.Common;
using Dapper;

namespace Api.Features.Employees;

internal sealed class EmployeeManager(
    ITenantDb db,
    ILogger<EmployeeManager> logger) : IEmployeeManager
{
    public async Task<EmployeeDto> CreateAsync(CreateEmployeeRequest request, CancellationToken ct = default)
    {
        using var conn = await db.CreateAsync();

        var sql = """
            INSERT INTO dbo.Employees (FirstName, LastName, Email, Phone, Role, IsActive)
            OUTPUT INSERTED.Id, INSERTED.FirstName, INSERTED.LastName, INSERTED.Email,
                   INSERTED.Phone, INSERTED.Role, INSERTED.IsActive, INSERTED.CreatedAt
            VALUES (@FirstName, @LastName, @Email, @Phone, @Role, @IsActive)
            """;

        logger.LogInformation("Creating employee: {FirstName} {LastName}", request.FirstName, request.LastName);

        return await conn.QuerySingleAsync<EmployeeDto>(
            new CommandDefinition(sql, request, cancellationToken: ct));
    }

    public async Task<EmployeeDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        using var conn = await db.CreateAsync();

        var sql = """
            SELECT Id, FirstName, LastName, Email, Phone, Role, IsActive, CreatedAt
            FROM dbo.Employees
            WHERE Id = @Id
            """;

        return await conn.QuerySingleOrDefaultAsync<EmployeeDto>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }

    public async Task<EmployeeListResponse> ListAsync(int page, int pageSize, string? role, CancellationToken ct = default)
    {
        using var conn = await db.CreateAsync();

        var whereClause = string.IsNullOrWhiteSpace(role)
            ? ""
            : "WHERE Role = @Role";

        var countSql = $"SELECT COUNT(*) FROM dbo.Employees {whereClause}";

        var listSql = $"""
            SELECT Id, FirstName, LastName, Email, Phone, Role, IsActive, CreatedAt
            FROM dbo.Employees
            {whereClause}
            ORDER BY CreatedAt DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """;

        var parameters = new
        {
            Role = role,
            Offset = (page - 1) * pageSize,
            PageSize = pageSize
        };

        var totalCount = await conn.ExecuteScalarAsync<int>(
            new CommandDefinition(countSql, parameters, cancellationToken: ct));

        var items = await conn.QueryAsync<EmployeeDto>(
            new CommandDefinition(listSql, parameters, cancellationToken: ct));

        return new EmployeeListResponse(items.ToList(), totalCount);
    }

    public async Task<EmployeeDto?> UpdateAsync(Guid id, UpdateEmployeeRequest request, CancellationToken ct = default)
    {
        using var conn = await db.CreateAsync();

        var sql = """
            UPDATE dbo.Employees
            SET FirstName = @FirstName, LastName = @LastName, Email = @Email,
                Phone = @Phone, Role = @Role, IsActive = @IsActive,
                UpdatedAt = SYSUTCDATETIME()
            OUTPUT INSERTED.Id, INSERTED.FirstName, INSERTED.LastName, INSERTED.Email,
                   INSERTED.Phone, INSERTED.Role, INSERTED.IsActive, INSERTED.CreatedAt
            WHERE Id = @Id
            """;

        logger.LogInformation("Updating employee {Id}", id);

        return await conn.QuerySingleOrDefaultAsync<EmployeeDto>(
            new CommandDefinition(sql, new
            {
                Id = id,
                request.FirstName,
                request.LastName,
                request.Email,
                request.Phone,
                request.Role,
                request.IsActive
            }, cancellationToken: ct));
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        using var conn = await db.CreateAsync();

        var sql = "DELETE FROM dbo.Employees WHERE Id = @Id";

        logger.LogInformation("Deleting employee {Id}", id);

        var rows = await conn.ExecuteAsync(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));

        return rows > 0;
    }
}
