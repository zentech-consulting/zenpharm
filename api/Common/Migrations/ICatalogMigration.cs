namespace Api.Common.Migrations;

public interface ICatalogMigration
{
    Task RunAllAsync(CancellationToken ct = default);
}
