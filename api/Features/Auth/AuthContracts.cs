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
    string? FullName,
    string Role);

internal sealed record RefreshTokenJoinResult
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public DateTimeOffset ExpiresAt { get; init; }
    public bool IsRevoked { get; init; }
    public string Username { get; init; } = "";
    public string? Email { get; init; }
    public string? FullName { get; init; }
    public string Role { get; init; } = "";
    public bool IsActive { get; init; }
}

// --- Session Control DTOs ---

public sealed record ActiveSessionDto(
    Guid Id,
    string Username,
    string? CreatedByIp,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastUsedAt);

public sealed record SessionSummaryDto(
    int ActiveSessions,
    int MaxSessions,
    string PlanName);

public sealed record SessionListResponse(
    IReadOnlyList<ActiveSessionDto> Sessions,
    int MaxSessions);

internal sealed record AdminUserEntity
{
    public Guid Id { get; init; }
    public string Username { get; init; } = "";
    public string PasswordHash { get; init; } = "";
    public string? Email { get; init; }
    public string? FullName { get; init; }
    public string Role { get; init; } = "Admin";
    public bool IsActive { get; init; } = true;
    public int FailedLoginAttempts { get; init; }
    public DateTimeOffset? LockoutEnd { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? LastLoginAt { get; init; }
    public string? LastLoginIp { get; init; }
}
