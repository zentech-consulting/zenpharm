using System.Security.Claims;
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
            var result = await mgr.LoginAsync(req, clientIp, ct);
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

        return app;
    }
}
