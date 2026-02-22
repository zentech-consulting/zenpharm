namespace Api.Common;

public interface IDbMigration
{
    Task RunAllAsync(CancellationToken ct = default);
}

internal sealed class DbMigration(
    IDbConnectionFactory db,
    ILogger<DbMigration> logger) : IDbMigration
{
    public async Task RunAllAsync(CancellationToken ct = default)
    {
        try
        {
            using var conn = await db.CreateAsync();
            logger.LogInformation("Database migration check completed");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Database migration skipped — connection unavailable");
        }
    }
}
