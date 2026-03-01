namespace Api.Common.Tenancy;

public static class TenancyServiceExtensions
{
    /// <summary>
    /// Registers multi-tenancy services: ICatalogDb (singleton), ITenantResolver (singleton),
    /// ITenantDb (scoped, from TenantContext), and TenantContext (scoped, from HttpContext).
    /// </summary>
    public static IServiceCollection AddMultiTenancy(this IServiceCollection services, IConfiguration configuration)
    {
        // Catalog DB — singleton, connects to the platform-wide tenant registry
        var catalogConnStr = configuration.GetConnectionString("CatalogConnection") ?? "";
        var catalogFactory = new SqlConnectionFactory(catalogConnStr);
        services.AddSingleton<ICatalogDb>(catalogFactory);

        // Tenant resolver — singleton with in-memory cache
        services.AddSingleton<ITenantResolver, TenantResolver>();

        // HttpContext accessor for scoped services
        services.AddHttpContextAccessor();

        // TenantContext — scoped, nullable (null when no tenant resolved e.g. health check).
        // The nullable generic constraint warning is expected; TenantContext is intentionally
        // nullable in the DI container since not all requests have a resolved tenant.
#pragma warning disable CS8634
        services.AddScoped<TenantContext?>(sp =>
        {
            var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
            return httpContextAccessor.HttpContext?.GetTenantContext();
        });
#pragma warning restore CS8634

        // ITenantDb — scoped, connection string from resolved TenantContext
        services.AddScoped<ITenantDb>(sp =>
        {
            var tenant = sp.GetService<TenantContext>();
            if (tenant is not null)
                return new TenantSqlConnectionFactory(tenant.ConnectionString);

            // Dev fallback: use DefaultConnection when no tenant resolved
            var fallbackConnStr = configuration.GetConnectionString("DefaultConnection") ?? "";
            return new SqlConnectionFactory(fallbackConnStr);
        });

        return services;
    }

    /// <summary>
    /// Adds TenantMiddleware to the request pipeline.
    /// Place after UseCors() and before UseRateLimiter().
    /// </summary>
    public static IApplicationBuilder UseTenantResolution(this IApplicationBuilder app)
    {
        return app.UseMiddleware<TenantMiddleware>();
    }
}
