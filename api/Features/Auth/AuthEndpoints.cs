using System.Security.Claims;
using Api.Common.Tenancy;
using Microsoft.AspNetCore.Http;

namespace Api.Features.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/auth").WithTags("Authentication");

        g.MapPost("login", async Task<IResult> (LoginRequest req, IAuthManager mgr, HttpContext ctx, CancellationToken ct) =>
        {
            var clientIp = ctx.Connection.RemoteIpAddress?.ToString();
            var tenantId = (ctx.Items["TenantContext"] as TenantContext)?.TenantId;
            var result = await mgr.LoginAsync(req, clientIp, tenantId, ct);
            return result is not null ? Results.Ok(result) : Results.Unauthorized();
        })
        .AllowAnonymous()
        .RequireRateLimiting("auth-login")
        .WithOpenApi(op => { op.Summary = "Authenticate with username and password"; return op; });

        g.MapPost("refresh", async Task<IResult> (RefreshTokenRequest req, IAuthManager mgr, CancellationToken ct) =>
        {
            var result = await mgr.RefreshAccessTokenAsync(req.RefreshToken, ct);
            return result is not null ? Results.Ok(result) : Results.Unauthorized();
        })
        .AllowAnonymous()
        .RequireRateLimiting("auth-login")
        .WithOpenApi(op => { op.Summary = "Refresh an expired access token"; return op; });

        g.MapPost("logout", async Task<IResult> (RefreshTokenRequest req, IAuthManager mgr, CancellationToken ct) =>
        {
            var revoked = await mgr.RevokeRefreshTokenAsync(req.RefreshToken, ct);
            return revoked ? Results.Ok() : Results.BadRequest();
        })
        .RequireAuthorization()
        .WithOpenApi(op => { op.Summary = "Revoke a refresh token"; return op; });

        g.MapGet("me", async Task<IResult> (IAuthManager mgr, HttpContext ctx, CancellationToken ct) =>
        {
            var sub = ctx.User.FindFirstValue("sub");
            if (sub is null || !Guid.TryParse(sub, out var userId))
                return Results.Unauthorized();

            var user = await mgr.GetCurrentUserAsync(userId, ct);
            return user is not null ? Results.Ok(user) : Results.Unauthorized();
        })
        .RequireAuthorization()
        .WithOpenApi(op => { op.Summary = "Get current authenticated user"; return op; });

        // --- Session Management Endpoints ---

        g.MapGet("sessions", async Task<IResult> (IAuthManager mgr, HttpContext ctx, CancellationToken ct) =>
        {
            var role = ctx.User.FindFirstValue(ClaimTypes.Role);
            if (role is not ("Admin" or "SuperAdmin" or "Manager"))
                return Results.Forbid();

            var tenantContext = ctx.Items["TenantContext"] as TenantContext;
            if (tenantContext is null) return Results.BadRequest("Tenant context required");

            var result = await mgr.GetActiveSessionsAsync(tenantContext.TenantId, ct);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        })
        .RequireAuthorization()
        .WithOpenApi(op => { op.Summary = "List active sessions (admin/manager only)"; return op; });

        g.MapGet("sessions/summary", async Task<IResult> (IAuthManager mgr, HttpContext ctx, CancellationToken ct) =>
        {
            var role = ctx.User.FindFirstValue(ClaimTypes.Role);
            if (role is not ("Admin" or "SuperAdmin" or "Manager"))
                return Results.Forbid();

            var tenantContext = ctx.Items["TenantContext"] as TenantContext;
            if (tenantContext is null) return Results.BadRequest("Tenant context required");

            var result = await mgr.GetSessionSummaryAsync(tenantContext.TenantId, ct);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        })
        .RequireAuthorization()
        .WithOpenApi(op => { op.Summary = "Get active session count vs plan limit (admin/manager only)"; return op; });

        g.MapDelete("sessions/{id:guid}", async Task<IResult> (Guid id, IAuthManager mgr, HttpContext ctx, CancellationToken ct) =>
        {
            var role = ctx.User.FindFirstValue(ClaimTypes.Role);
            if (role is not ("Admin" or "SuperAdmin" or "Manager"))
                return Results.Forbid();

            var revoked = await mgr.RevokeSessionByIdAsync(id, ct);
            return revoked ? Results.Ok() : Results.NotFound();
        })
        .RequireAuthorization()
        .WithOpenApi(op => { op.Summary = "Revoke a specific session (admin/manager only)"; return op; });

        return app;
    }
}
