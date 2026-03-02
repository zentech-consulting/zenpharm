namespace Api.Features.Auth;

public interface IAuthManager
{
    Task<LoginResponse?> LoginAsync(LoginRequest request, string? clientIp, Guid? tenantId = null, CancellationToken ct = default);
    Task<RefreshTokenResponse?> RefreshAccessTokenAsync(string refreshToken, CancellationToken ct = default);
    Task<bool> RevokeRefreshTokenAsync(string refreshToken, CancellationToken ct = default);
    Task<AdminUserDto?> GetCurrentUserAsync(Guid userId, CancellationToken ct = default);
    Task<SessionListResponse?> GetActiveSessionsAsync(Guid tenantId, CancellationToken ct = default);
    Task<SessionSummaryDto?> GetSessionSummaryAsync(Guid tenantId, CancellationToken ct = default);
    Task<bool> RevokeSessionByIdAsync(Guid sessionId, CancellationToken ct = default);
}
