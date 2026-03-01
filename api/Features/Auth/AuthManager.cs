using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Api.Common;
using Dapper;
using Microsoft.IdentityModel.Tokens;

namespace Api.Features.Auth;

internal sealed class AuthManager(
    ITenantDb db,
    IConfiguration cfg,
    ILogger<AuthManager> logger) : IAuthManager
{
    private readonly byte[] _secretKey = Encoding.UTF8.GetBytes(
        cfg["Jwt:SecretKey"] is { Length: >= 32 } key
            ? key
            : throw new InvalidOperationException("Jwt:SecretKey must be at least 32 characters"));

    private readonly string _issuer = cfg["Jwt:Issuer"] ?? "zenpharm";
    private readonly string _audience = cfg["Jwt:Audience"] ?? "zenpharm-clients";
    private readonly int _accessTokenMinutes = int.Parse(cfg["Jwt:AccessTokenMinutes"] ?? "60");
    private readonly int _refreshTokenDays = int.Parse(cfg["Jwt:RefreshTokenDays"] ?? "7");
    private readonly int _refreshTokenDaysRememberMe = int.Parse(cfg["Jwt:RememberMeRefreshTokenDays"] ?? "30");
    private readonly int _maxFailedAttempts = int.Parse(cfg["Security:MaxFailedLoginAttempts"] ?? "5");
    private readonly int _lockoutMinutes = int.Parse(cfg["Security:LockoutDurationMinutes"] ?? "30");

    // ================================================================
    // Login
    // ================================================================
    public async Task<LoginResponse?> LoginAsync(
        LoginRequest request,
        string? clientIp,
        CancellationToken ct = default)
    {
        logger.LogInformation("Login attempt. Username={Username} IP={IP}",
            request.Username, clientIp ?? "unknown");

        using var conn = await db.CreateAsync();

        // 1. Look up user
        const string sqlSelect = """
            SELECT Id, Username, PasswordHash, Email, FullName, Role,
                   IsActive, FailedLoginAttempts, LockoutEnd
            FROM dbo.AdminUsers
            WHERE Username = @Username;
            """;

        var user = await conn.QueryFirstOrDefaultAsync<AdminUserEntity>(
            new CommandDefinition(sqlSelect, new { request.Username }, cancellationToken: ct));

        if (user is null)
        {
            logger.LogWarning("Login failed. User not found. Username={Username}", request.Username);
            await Task.Delay(Random.Shared.Next(100, 300), ct);
            return null;
        }

        // 2. Check account status (delay all rejection paths to prevent timing-based enumeration)
        if (!user.IsActive)
        {
            logger.LogWarning("Login failed. Account inactive. UserId={UserId}", user.Id);
            await Task.Delay(Random.Shared.Next(100, 300), ct);
            return null;
        }

        if (user.LockoutEnd > DateTimeOffset.UtcNow)
        {
            logger.LogWarning("Login failed. Account locked until {LockoutEnd}. UserId={UserId}",
                user.LockoutEnd, user.Id);
            await Task.Delay(Random.Shared.Next(100, 300), ct);
            return null;
        }

        // 3. Verify password
        bool passwordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);

        if (!passwordValid)
        {
            await RecordFailedLoginAsync(conn, user.Id, ct);

            logger.LogWarning("Login failed. Invalid password. UserId={UserId} Attempts={Attempts}",
                user.Id, user.FailedLoginAttempts + 1);

            await Task.Delay(Random.Shared.Next(100, 300), ct);
            return null;
        }

        // 4. Reset lockout, record successful login
        await RecordSuccessfulLoginAsync(conn, user.Id, clientIp, ct);

        // 5. Generate tokens
        var (accessToken, expiresAt) = GenerateAccessToken(user);
        var refreshToken = GenerateRefreshToken();
        var refreshTokenExpiry = DateTimeOffset.UtcNow.AddDays(
            request.RememberMe ? _refreshTokenDaysRememberMe : _refreshTokenDays);

        // 6. Store refresh token hash
        await StoreRefreshTokenAsync(conn, user.Id, refreshToken, refreshTokenExpiry, clientIp, ct);

        logger.LogInformation("Login successful. UserId={UserId} RememberMe={RememberMe}",
            user.Id, request.RememberMe);

        return new LoginResponse(
            AccessToken: accessToken,
            RefreshToken: refreshToken,
            ExpiresAt: expiresAt,
            User: new AdminUserDto(
                Id: user.Id,
                Username: user.Username,
                Email: user.Email,
                FullName: user.FullName,
                Role: user.Role));
    }

    // ================================================================
    // Refresh Access Token
    // ================================================================
    public async Task<RefreshTokenResponse?> RefreshAccessTokenAsync(
        string refreshToken,
        CancellationToken ct = default)
    {
        logger.LogInformation("Refresh token attempt");

        using var conn = await db.CreateAsync();
        var tokenHash = ComputeSha256Hash(refreshToken);

        const string sqlToken = """
            SELECT rt.Id, rt.UserId, rt.ExpiresAt, rt.IsRevoked,
                   u.Username, u.Email, u.FullName, u.Role, u.IsActive
            FROM dbo.RefreshTokens rt
            INNER JOIN dbo.AdminUsers u ON rt.UserId = u.Id
            WHERE rt.TokenHash = @TokenHash;
            """;

        var tokenRecord = await conn.QueryFirstOrDefaultAsync<RefreshTokenJoinResult>(
            new CommandDefinition(sqlToken, new { TokenHash = tokenHash }, cancellationToken: ct));

        if (tokenRecord is null)
        {
            logger.LogWarning("Refresh failed. Token not found");
            return null;
        }

        if (tokenRecord.IsRevoked)
        {
            logger.LogWarning("Refresh failed. Token revoked. UserId={UserId}", tokenRecord.UserId);
            return null;
        }

        if (tokenRecord.ExpiresAt < DateTimeOffset.UtcNow)
        {
            logger.LogWarning("Refresh failed. Token expired. UserId={UserId}", tokenRecord.UserId);
            return null;
        }

        if (!tokenRecord.IsActive)
        {
            logger.LogWarning("Refresh failed. User inactive. UserId={UserId}", tokenRecord.UserId);
            return null;
        }

        // Generate new access token only (keep same refresh token)
        var user = new AdminUserEntity
        {
            Id = tokenRecord.UserId,
            Username = tokenRecord.Username,
            Email = tokenRecord.Email,
            FullName = tokenRecord.FullName,
            Role = tokenRecord.Role
        };

        var (newAccessToken, newExpiresAt) = GenerateAccessToken(user);

        logger.LogInformation("Token refreshed successfully. UserId={UserId}", user.Id);

        return new RefreshTokenResponse(
            AccessToken: newAccessToken,
            RefreshToken: refreshToken,
            ExpiresAt: newExpiresAt);
    }

    // ================================================================
    // Revoke (Logout)
    // ================================================================
    public async Task<bool> RevokeRefreshTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        logger.LogInformation("Revoke refresh token");

        using var conn = await db.CreateAsync();
        var tokenHash = ComputeSha256Hash(refreshToken);

        const string sql = """
            UPDATE dbo.RefreshTokens
            SET IsRevoked = 1, RevokedAt = SYSUTCDATETIME()
            WHERE TokenHash = @TokenHash AND IsRevoked = 0;
            """;

        var affected = await conn.ExecuteAsync(
            new CommandDefinition(sql, new { TokenHash = tokenHash }, cancellationToken: ct));

        return affected > 0;
    }

    // ================================================================
    // Get Current User
    // ================================================================
    public async Task<AdminUserDto?> GetCurrentUserAsync(Guid userId, CancellationToken ct = default)
    {
        using var conn = await db.CreateAsync();

        const string sql = """
            SELECT Id, Username, Email, FullName, Role
            FROM dbo.AdminUsers
            WHERE Id = @UserId AND IsActive = 1;
            """;

        return await conn.QueryFirstOrDefaultAsync<AdminUserDto>(
            new CommandDefinition(sql, new { UserId = userId }, cancellationToken: ct));
    }

    // ================================================================
    // Internal Static Helpers (testable via InternalsVisibleTo)
    // ================================================================

    internal (string Token, DateTimeOffset ExpiresAt) GenerateAccessToken(AdminUserEntity user)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_accessTokenMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? ""),
            new Claim("fullName", user.FullName ?? ""),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())
        };

        var key = new SymmetricSecurityKey(_secretKey);
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    internal static string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    internal static string ComputeSha256Hash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(bytes);
    }

    // ================================================================
    // Private DB Helpers
    // ================================================================

    private async Task RecordFailedLoginAsync(IDbConnection conn, Guid userId, CancellationToken ct)
    {
        const string sql = """
            UPDATE dbo.AdminUsers
            SET FailedLoginAttempts = FailedLoginAttempts + 1,
                LockoutEnd = CASE
                    WHEN FailedLoginAttempts + 1 >= @MaxAttempts
                    THEN DATEADD(MINUTE, @LockoutMinutes, SYSUTCDATETIME())
                    ELSE LockoutEnd
                END,
                UpdatedAt = SYSUTCDATETIME()
            WHERE Id = @UserId;
            """;

        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            UserId = userId,
            MaxAttempts = _maxFailedAttempts,
            LockoutMinutes = _lockoutMinutes
        }, cancellationToken: ct));
    }

    private static async Task RecordSuccessfulLoginAsync(
        IDbConnection conn, Guid userId, string? clientIp, CancellationToken ct)
    {
        const string sql = """
            UPDATE dbo.AdminUsers
            SET FailedLoginAttempts = 0,
                LockoutEnd = NULL,
                LastLoginAt = SYSUTCDATETIME(),
                LastLoginIp = @ClientIp,
                UpdatedAt = SYSUTCDATETIME()
            WHERE Id = @UserId;
            """;

        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            UserId = userId,
            ClientIp = clientIp
        }, cancellationToken: ct));
    }

    private static async Task StoreRefreshTokenAsync(
        IDbConnection conn, Guid userId, string token,
        DateTimeOffset expiresAt, string? clientIp, CancellationToken ct)
    {
        var tokenHash = ComputeSha256Hash(token);

        const string sql = """
            INSERT INTO dbo.RefreshTokens (UserId, TokenHash, ExpiresAt, CreatedByIp)
            VALUES (@UserId, @TokenHash, @ExpiresAt, @CreatedByIp);
            """;

        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
            CreatedByIp = clientIp
        }, cancellationToken: ct));
    }
}
