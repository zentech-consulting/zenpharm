using Api.Common;
using Api.Common.Tenancy;

namespace Api.Features.Auth;

internal sealed class AuthManager(
    ITenantDb db,
    TenantContext? tenant,
    IConfiguration cfg,
    ILogger<AuthManager> logger) : IAuthManager
{
    public Task<LoginResponse?> LoginAsync(LoginRequest request, string? clientIp, CancellationToken ct = default)
    {
        throw new NotImplementedException("Auth module not yet implemented — see Phase 1, Subtask 2");
    }

    public Task<RefreshTokenResponse?> RefreshAccessTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        throw new NotImplementedException("Auth module not yet implemented — see Phase 1, Subtask 2");
    }

    public Task<bool> RevokeRefreshTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        throw new NotImplementedException("Auth module not yet implemented — see Phase 1, Subtask 2");
    }

    public Task<AdminUserDto?> GetCurrentUserAsync(Guid userId, CancellationToken ct = default)
    {
        throw new NotImplementedException("Auth module not yet implemented — see Phase 1, Subtask 2");
    }
}
