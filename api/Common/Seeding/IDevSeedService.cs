namespace Api.Common.Seeding;

internal interface IDevSeedService
{
    Task SeedAsync(CancellationToken ct = default);
}
