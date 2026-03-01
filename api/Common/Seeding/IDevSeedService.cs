namespace Api.Common.Seeding;

public interface IDevSeedService
{
    Task SeedAsync(CancellationToken ct = default);
}
