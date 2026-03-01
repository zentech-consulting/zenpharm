namespace Api.Features.Auth;

public interface IAuthManager
{
    Task<LoginResponse?> LoginAsync(LoginRequest request, string? clientIp, CancellationToken ct = default);
    Task<RefreshTokenResponse?> RefreshAccessTokenAsync(string refreshToken, CancellationToken ct = default);
    Task<bool> RevokeRefreshTokenAsync(string refreshToken, CancellationToken ct = default);
    Task<AdminUserDto?> GetCurrentUserAsync(Guid userId, CancellationToken ct = default);
}
