using System.Text;
using Api.Common;
using Api.Features.AiChat;
using Api.Features.AiChat.Tools;
using Api.Features.Auth;
using Api.Features.Bookings;
using Api.Features.Clients;
using Api.Features.Employees;
using Api.Features.Knowledge;
using Api.Features.Notifications;
using Api.Features.Reports;
using Api.Features.Schedules;
using Api.Features.Services;
using Dapper;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// --- Swagger / OpenAPI ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opts =>
{
    opts.SwaggerDoc("v1", new() { Title = "Zentech Biz API", Version = "v1" });
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

var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "zentech-biz";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "zentech-biz-clients";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
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

// --- Database ---
var connStr = builder.Configuration.GetConnectionString("DefaultConnection") ?? "";
builder.Services.AddSingleton<IDbConnectionFactory>(new SqlConnectionFactory(connStr));
builder.Services.AddScoped<IDbMigration, DbMigration>();

// --- Feature Managers ---
builder.Services.AddScoped<IAuthManager, AuthManager>();
builder.Services.AddScoped<IClientManager, ClientManager>();
builder.Services.AddScoped<IServiceManager, ServiceManager>();
builder.Services.AddScoped<IBookingManager, BookingManager>();
builder.Services.AddScoped<IScheduleManager, ScheduleManager>();
builder.Services.AddScoped<IEmployeeManager, EmployeeManager>();
builder.Services.AddScoped<IAiChatManager, AiChatManager>();
builder.Services.AddSingleton<IAiToolExecutor, AiToolExecutor>();
builder.Services.AddScoped<IKnowledgeManager, KnowledgeManager>();
builder.Services.AddScoped<IEmailService, StubEmailService>();
builder.Services.AddScoped<IReportManager, ReportManager>();

// --- HTTP Clients ---
builder.Services.AddHttpClient("Anthropic", client =>
{
    client.BaseAddress = new Uri("https://api.anthropic.com");
    client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
});

var app = builder.Build();

// --- Database Migration ---
using (var scope = app.Services.CreateScope())
{
    var migration = scope.ServiceProvider.GetRequiredService<IDbMigration>();
    await migration.RunAllAsync();
}

// --- Middleware ---
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// --- Health Check ---
app.MapGet("/health", async (IDbConnectionFactory db) =>
{
    bool dbOk;
    long latencyMs;

    try
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        using var conn = await db.CreateAsync();
        await conn.ExecuteScalarAsync<int>("SELECT 1");
        sw.Stop();
        dbOk = true;
        latencyMs = sw.ElapsedMilliseconds;
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Health check: database probe failed");
        dbOk = false;
        latencyMs = -1;
    }

    var result = new { ok = dbOk, ts = DateTimeOffset.UtcNow, db = new { ok = dbOk, latencyMs } };
    return dbOk ? Results.Ok(result) : Results.Json(result, statusCode: 503);
})
.WithTags("Health")
.AllowAnonymous()
.WithOpenApi(op => { op.Summary = "Health check with database probe"; return op; });

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

app.Run();
