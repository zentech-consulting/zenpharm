using System.Text;
using Api.Common;
using Api.Common.Migrations;
using Api.Common.Seeding;
using Api.Common.Tenancy;
using Api.Features.AiChat;
using Api.Features.AiChat.Tools;
using Api.Features.Auth;
using Api.Features.Bookings;
using Api.Features.Clients;
using Api.Features.Employees;
using Api.Features.Knowledge;
using Api.Features.MasterProducts;
using Api.Features.Notifications;
using Api.Features.Platform;
using Api.Features.Products;
using Api.Features.Reports;
using Api.Features.Schedules;
using Api.Features.Services;
using Dapper;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// --- Dapper Type Handlers (DateOnly/TimeOnly) ---
Dapper.SqlMapper.AddTypeHandler(new Api.Common.DateOnlyTypeHandler());
Dapper.SqlMapper.AddTypeHandler(new Api.Common.NullableDateOnlyTypeHandler());
Dapper.SqlMapper.AddTypeHandler(new Api.Common.TimeOnlyTypeHandler());
Dapper.SqlMapper.AddTypeHandler(new Api.Common.NullableTimeOnlyTypeHandler());

// --- Swagger / OpenAPI ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opts =>
{
    opts.SwaggerDoc("v1", new() { Title = "ZenPharm API", Version = "v1" });
    opts.AddSecurityDefinition("Bearer", new()
    {
        Description = "JWT Bearer token. Enter: Bearer {token}",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    opts.AddSecurityRequirement(new()
    {
        {
            new()
            {
                Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// --- JWT Authentication ---
var jwtKey = builder.Configuration["Jwt:SecretKey"]
    ?? throw new InvalidOperationException(
        "Jwt:SecretKey is not configured. Use 'dotnet user-secrets set \"Jwt:SecretKey\" \"<min-32-chars>\"' to set it.");
if (jwtKey.Length < 32)
    throw new InvalidOperationException("Jwt:SecretKey must be at least 32 characters.");

var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "zenpharm";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "zenpharm-clients";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.MapInboundClaims = false;
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });
builder.Services.AddAuthorization();

// --- Rate Limiting ---
builder.Services.AddRateLimiter(opts =>
{
    opts.AddFixedWindowLimiter("auth-login", o =>
    {
        o.PermitLimit = 5;
        o.Window = TimeSpan.FromMinutes(1);
    });
    opts.AddFixedWindowLimiter("ai-chat", o =>
    {
        o.PermitLimit = 10;
        o.Window = TimeSpan.FromMinutes(1);
    });
    opts.RejectionStatusCode = 429;
});

// --- CORS ---
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? (builder.Environment.IsDevelopment()
        ? new[] { "http://localhost:51000", "http://localhost:51001" }
        : Array.Empty<string>());

builder.Services.AddCors(opts =>
{
    opts.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// --- Multi-Tenancy (Catalog DB + Tenant DB) ---
builder.Services.AddMultiTenancy(builder.Configuration);

// --- Migrations ---
builder.Services.AddSingleton<ICatalogMigration, CatalogMigration>();
builder.Services.AddSingleton<ITenantMigration, TenantMigration>();

// --- Feature Managers ---
builder.Services.AddScoped<IAuthManager, AuthManager>();
builder.Services.AddScoped<IClientManager, ClientManager>();
builder.Services.AddScoped<IServiceManager, ServiceManager>();
builder.Services.AddScoped<IBookingManager, BookingManager>();
builder.Services.AddScoped<IScheduleManager, ScheduleManager>();
builder.Services.AddScoped<IEmployeeManager, EmployeeManager>();
builder.Services.AddScoped<IAiChatManager, AiChatManager>();
builder.Services.AddScoped<IAiToolExecutor, AiToolExecutor>();
builder.Services.AddScoped<IKnowledgeManager, KnowledgeManager>();
var emailDryRun = builder.Configuration.GetValue("Email:DryRun", true);
if (emailDryRun)
    builder.Services.AddScoped<IEmailService, DryRunEmailService>();
else
    builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IReportManager, ReportManager>();
builder.Services.AddScoped<IMasterProductManager, MasterProductManager>();
builder.Services.AddScoped<IProductManager, ProductManager>();
builder.Services.AddSingleton<IProvisioningPipeline, ProvisioningPipeline>();

// --- Dev Seed (Development only) ---
if (builder.Environment.IsDevelopment())
    builder.Services.AddSingleton<IDevSeedService, DevSeedService>();

// --- HTTP Clients ---
builder.Services.AddHttpClient("Anthropic", client =>
{
    client.BaseAddress = new Uri("https://api.anthropic.com");
    client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
});

var app = builder.Build();

// --- Catalogue Database Migration + Dev Seed ---
using (var scope = app.Services.CreateScope())
{
    var catalogMigration = scope.ServiceProvider.GetRequiredService<ICatalogMigration>();
    await catalogMigration.RunAllAsync();

    if (app.Environment.IsDevelopment())
    {
        var devSeed = scope.ServiceProvider.GetService<IDevSeedService>();
        if (devSeed is not null)
            await devSeed.SeedAsync();
    }
}

// --- Middleware ---
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseTenantResolution();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// --- Health Check ---
app.MapGet("/health", async (ICatalogDb catalogDb) =>
{
    bool dbOk;
    long latencyMs;

    try
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        using var conn = await catalogDb.CreateAsync();
        await conn.ExecuteScalarAsync<int>("SELECT 1");
        sw.Stop();
        dbOk = true;
        latencyMs = sw.ElapsedMilliseconds;
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Health check: catalogue database probe failed");
        dbOk = false;
        latencyMs = -1;
    }

    var result = new { ok = dbOk, ts = DateTimeOffset.UtcNow, db = new { ok = dbOk, latencyMs } };
    return dbOk ? Results.Ok(result) : Results.Json(result, statusCode: 503);
})
.WithTags("Health")
.AllowAnonymous()
.WithOpenApi(op => { op.Summary = "Health check with catalogue database probe"; return op; });

// --- Feature Endpoints ---
app.MapAuthEndpoints();
app.MapClientEndpoints();
app.MapServiceEndpoints();
app.MapBookingEndpoints();
app.MapScheduleEndpoints();
app.MapEmployeeEndpoints();
app.MapAiChatEndpoints();
app.MapKnowledgeEndpoints();
app.MapReportEndpoints();
app.MapNotificationEndpoints();
app.MapMasterProductEndpoints();
app.MapProductEndpoints();
app.MapPlatformEndpoints();
app.MapStripeWebhookEndpoints();

// --- DryRun Safety Check (non-Development) ---
if (!app.Environment.IsDevelopment())
{
    if (app.Configuration.GetValue("Email:DryRun", true))
        app.Logger.LogWarning("Email:DryRun is TRUE in {Environment} — emails will be discarded", app.Environment.EnvironmentName);
    if (app.Configuration.GetValue("SmsBroadcast:DryRun", true))
        app.Logger.LogWarning("SmsBroadcast:DryRun is TRUE in {Environment} — SMS will be discarded", app.Environment.EnvironmentName);
}

app.Run();
