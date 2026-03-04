using Api.Common;
using Dapper;

namespace Api.Features.Platform;

internal sealed class PbsSyncManager(
    ICatalogDb catalogDb,
    ILogger<PbsSyncManager> logger) : IPbsSyncManager
{
    public async Task<PbsSyncResult> SyncAsync(CancellationToken ct = default)
    {
        using var conn = await catalogDb.CreateAsync();

        var products = (await conn.QueryAsync<(Guid Id, string? ActiveIngredients, string? PbsItemCode)>(
            new CommandDefinition(
                "SELECT Id, ActiveIngredients, PbsItemCode FROM dbo.MasterProducts WHERE IsActive = 1",
                cancellationToken: ct))).ToArray();

        var updated = 0;
        foreach (var product in products)
        {
            var pbsCode = PbsCodeMapping.FindPbsCode(product.ActiveIngredients);
            if (pbsCode is null) continue;

            // Only update if different from current value
            if (string.Equals(product.PbsItemCode, pbsCode, StringComparison.OrdinalIgnoreCase))
                continue;

            await conn.ExecuteAsync(
                new CommandDefinition(
                    "UPDATE dbo.MasterProducts SET PbsItemCode = @PbsCode, UpdatedAt = SYSUTCDATETIME() WHERE Id = @Id",
                    new { PbsCode = pbsCode, Id = product.Id }, cancellationToken: ct));
            updated++;
        }

        var matched = products.Count(p => PbsCodeMapping.FindPbsCode(p.ActiveIngredients) is not null);

        logger.LogInformation("PBS sync completed: {Total} products, {Matched} matched, {Updated} updated",
            products.Length, matched, updated);

        return new PbsSyncResult(products.Length, matched, updated);
    }

    public async Task<PbsSummary> GetSummaryAsync(CancellationToken ct = default)
    {
        using var conn = await catalogDb.CreateAsync();

        var total = await conn.ExecuteScalarAsync<int>(
            new CommandDefinition(
                "SELECT COUNT(*) FROM dbo.MasterProducts WHERE IsActive = 1",
                cancellationToken: ct));

        var withCode = await conn.ExecuteScalarAsync<int>(
            new CommandDefinition(
                "SELECT COUNT(*) FROM dbo.MasterProducts WHERE IsActive = 1 AND PbsItemCode IS NOT NULL AND PbsItemCode != ''",
                cancellationToken: ct));

        return new PbsSummary(total, withCode, total - withCode);
    }
}
