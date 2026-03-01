using Api.Common;
using Dapper;

namespace Api.Features.Clients;

internal sealed class ClientManager(
    ITenantDb db,
    ILogger<ClientManager> logger) : IClientManager
{
    public async Task<ClientDto> CreateAsync(CreateClientRequest request, CancellationToken ct = default)
    {
        using var conn = await db.CreateAsync();

        var sql = """
            INSERT INTO dbo.Clients (FirstName, LastName, Email, Phone, Notes, DateOfBirth, Allergies, MedicationNotes, Tags)
            OUTPUT INSERTED.Id, INSERTED.FirstName, INSERTED.LastName, INSERTED.Email,
                   INSERTED.Phone, INSERTED.Notes, INSERTED.DateOfBirth, INSERTED.Allergies,
                   INSERTED.MedicationNotes, INSERTED.Tags, INSERTED.CreatedAt
            VALUES (@FirstName, @LastName, @Email, @Phone, @Notes, @DateOfBirth, @Allergies, @MedicationNotes, @Tags)
            """;

        logger.LogInformation("Creating client: {FirstName} {LastName}", request.FirstName, request.LastName);

        var row = await conn.QuerySingleAsync<ClientDto>(
            new CommandDefinition(sql, request, cancellationToken: ct));

        return row;
    }

    public async Task<ClientDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        using var conn = await db.CreateAsync();

        var sql = """
            SELECT Id, FirstName, LastName, Email, Phone, Notes,
                   DateOfBirth, Allergies, MedicationNotes, Tags, CreatedAt
            FROM dbo.Clients
            WHERE Id = @Id
            """;

        return await conn.QuerySingleOrDefaultAsync<ClientDto>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }

    public async Task<ClientListResponse> ListAsync(int page, int pageSize, string? search, CancellationToken ct = default)
    {
        using var conn = await db.CreateAsync();

        var whereClause = string.IsNullOrWhiteSpace(search)
            ? ""
            : "WHERE FirstName LIKE @Search OR LastName LIKE @Search OR Email LIKE @Search";

        var countSql = $"SELECT COUNT(*) FROM dbo.Clients {whereClause}";

        var listSql = $"""
            SELECT Id, FirstName, LastName, Email, Phone, Notes,
                   DateOfBirth, Allergies, MedicationNotes, Tags, CreatedAt
            FROM dbo.Clients
            {whereClause}
            ORDER BY CreatedAt DESC
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

        var items = await conn.QueryAsync<ClientDto>(
            new CommandDefinition(listSql, parameters, cancellationToken: ct));

        return new ClientListResponse(items.ToList(), totalCount);
    }

    public async Task<ClientDto?> UpdateAsync(Guid id, UpdateClientRequest request, CancellationToken ct = default)
    {
        using var conn = await db.CreateAsync();

        var sql = """
            UPDATE dbo.Clients
            SET FirstName = @FirstName, LastName = @LastName, Email = @Email,
                Phone = @Phone, Notes = @Notes,
                DateOfBirth = @DateOfBirth, Allergies = @Allergies,
                MedicationNotes = @MedicationNotes, Tags = @Tags,
                UpdatedAt = SYSUTCDATETIME()
            OUTPUT INSERTED.Id, INSERTED.FirstName, INSERTED.LastName, INSERTED.Email,
                   INSERTED.Phone, INSERTED.Notes, INSERTED.DateOfBirth, INSERTED.Allergies,
                   INSERTED.MedicationNotes, INSERTED.Tags, INSERTED.CreatedAt
            WHERE Id = @Id
            """;

        logger.LogInformation("Updating client {Id}", id);

        return await conn.QuerySingleOrDefaultAsync<ClientDto>(
            new CommandDefinition(sql, new
            {
                Id = id,
                request.FirstName,
                request.LastName,
                request.Email,
                request.Phone,
                request.Notes,
                request.DateOfBirth,
                request.Allergies,
                request.MedicationNotes,
                request.Tags
            }, cancellationToken: ct));
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        using var conn = await db.CreateAsync();

        var sql = "DELETE FROM dbo.Clients WHERE Id = @Id";

        logger.LogInformation("Deleting client {Id}", id);

        var rows = await conn.ExecuteAsync(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));

        return rows > 0;
    }
}
