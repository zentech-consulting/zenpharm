using System.ComponentModel.DataAnnotations;

namespace Api.Features.Auth;

public sealed record LoginRequest
{
    [Required, MaxLength(100)]
    public string Username { get; init; } = "";

    [Required, MaxLength(100)]
    public string Password { get; init; } = "";

    public bool RememberMe { get; init; }
}

public sealed record LoginResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt,
    AdminUserDto User);

public sealed record RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; init; } = "";
}

public sealed record RefreshTokenResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt);

public sealed record AdminUserDto(
    Guid Id,
    string Username,
    string? Email,
    string? DisplayName,
    string Role);

internal sealed record AdminUserEntity
{
    public Guid Id { get; init; }
    public string Username { get; init; } = "";
    public string PasswordHash { get; init; } = "";
    public string? Email { get; init; }
    public string? DisplayName { get; init; }
    public string Role { get; init; } = "staff";
    public bool IsActive { get; init; } = true;
    public bool IsLocked { get; init; }
    public int FailedLoginAttempts { get; init; }
    public DateTimeOffset? LockoutEnd { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? LastLoginAt { get; init; }
}
