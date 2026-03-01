using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Api.Common;
using Api.Features.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Api.Tests.Auth;

public class AuthManagerTests
{
    private static readonly Dictionary<string, string?> JwtConfig = new()
    {
        ["Jwt:SecretKey"] = "ThisIsATestSecretKeyThatIsAtLeast32BytesLong!!",
        ["Jwt:Issuer"] = "test-issuer",
        ["Jwt:Audience"] = "test-audience",
        ["Jwt:AccessTokenMinutes"] = "60",
        ["Jwt:RefreshTokenDays"] = "7",
        ["Jwt:RememberMeRefreshTokenDays"] = "30"
    };

    private static IConfiguration BuildConfig(Dictionary<string, string?>? overrides = null)
    {
        var settings = new Dictionary<string, string?>(JwtConfig);
        if (overrides is not null)
        {
            foreach (var kv in overrides)
                settings[kv.Key] = kv.Value;
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();
    }

    private static AuthManager CreateManager(IConfiguration? config = null)
    {
        var tenantDb = Substitute.For<ITenantDb>();

        return new AuthManager(
            tenantDb,
            config ?? BuildConfig(),
            NullLogger<AuthManager>.Instance);
    }

    // ================================================================
    // Contract / DTO Tests
    // ================================================================

    [Fact]
    public void AdminUserDto_RecordEquality()
    {
        var id = Guid.NewGuid();
        var a = new AdminUserDto(id, "admin", "admin@test.com", "Admin User", "Admin");
        var b = new AdminUserDto(id, "admin", "admin@test.com", "Admin User", "Admin");

        Assert.Equal(a, b);
    }

    [Fact]
    public void AdminUserEntity_DefaultValues()
    {
        var entity = new AdminUserEntity();

        Assert.Equal("", entity.Username);
        Assert.Equal("", entity.PasswordHash);
        Assert.Null(entity.Email);
        Assert.Null(entity.FullName);
        Assert.Equal("Admin", entity.Role);
        Assert.True(entity.IsActive);
        Assert.Equal(0, entity.FailedLoginAttempts);
        Assert.Null(entity.LockoutEnd);
        Assert.Null(entity.LastLoginAt);
        Assert.Null(entity.LastLoginIp);
    }

    [Fact]
    public void RefreshTokenJoinResult_DefaultValues()
    {
        var result = new RefreshTokenJoinResult();

        Assert.Equal(Guid.Empty, result.Id);
        Assert.Equal(Guid.Empty, result.UserId);
        Assert.Equal("", result.Username);
        Assert.Null(result.Email);
        Assert.Null(result.FullName);
        Assert.Equal("", result.Role);
        Assert.False(result.IsRevoked);
        Assert.False(result.IsActive);
    }

    [Fact]
    public void LoginRequest_DefaultValues()
    {
        var req = new LoginRequest();

        Assert.Equal("", req.Username);
        Assert.Equal("", req.Password);
        Assert.False(req.RememberMe);
    }

    // ================================================================
    // ComputeSha256Hash Tests
    // ================================================================

    [Fact]
    public void ComputeSha256Hash_Deterministic_SameInputSameOutput()
    {
        var hash1 = AuthManager.ComputeSha256Hash("test-input");
        var hash2 = AuthManager.ComputeSha256Hash("test-input");

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void ComputeSha256Hash_DifferentInputs_DifferentHashes()
    {
        var hash1 = AuthManager.ComputeSha256Hash("input-a");
        var hash2 = AuthManager.ComputeSha256Hash("input-b");

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void ComputeSha256Hash_ReturnsBase64String()
    {
        var hash = AuthManager.ComputeSha256Hash("hello");

        // SHA256 produces 32 bytes → Base64 = 44 chars (with padding)
        Assert.Equal(44, hash.Length);

        // Verify it's valid Base64
        var bytes = Convert.FromBase64String(hash);
        Assert.Equal(32, bytes.Length);
    }

    // ================================================================
    // GenerateRefreshToken Tests
    // ================================================================

    [Fact]
    public void GenerateRefreshToken_Returns88CharBase64String()
    {
        var token = AuthManager.GenerateRefreshToken();

        // 64 bytes → Base64 = 88 chars (with padding)
        Assert.Equal(88, token.Length);

        // Verify it's valid Base64
        var bytes = Convert.FromBase64String(token);
        Assert.Equal(64, bytes.Length);
    }

    [Fact]
    public void GenerateRefreshToken_UniqueEachCall()
    {
        var token1 = AuthManager.GenerateRefreshToken();
        var token2 = AuthManager.GenerateRefreshToken();

        Assert.NotEqual(token1, token2);
    }

    // ================================================================
    // GenerateAccessToken Tests
    // ================================================================

    [Fact]
    public void GenerateAccessToken_ContainsExpectedClaims()
    {
        var manager = CreateManager();
        var user = new AdminUserEntity
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            Email = "test@example.com",
            FullName = "Test User",
            Role = "Admin"
        };

        var (token, _) = manager.GenerateAccessToken(user);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        Assert.Equal(user.Id.ToString(), jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal("testuser", jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.UniqueName).Value);
        Assert.Equal("test@example.com", jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value);
        Assert.Equal("Test User", jwt.Claims.First(c => c.Type == "fullName").Value);
        Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.Role && c.Value == "Admin");
        Assert.Contains(jwt.Claims, c => c.Type == JwtRegisteredClaimNames.Jti);
        Assert.Contains(jwt.Claims, c => c.Type == JwtRegisteredClaimNames.Iat);
    }

    [Fact]
    public void GenerateAccessToken_CorrectIssuerAndAudience()
    {
        var manager = CreateManager();
        var user = new AdminUserEntity
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            Role = "Staff"
        };

        var (token, _) = manager.GenerateAccessToken(user);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        Assert.Equal("test-issuer", jwt.Issuer);
        Assert.Contains("test-audience", jwt.Audiences);
    }

    [Fact]
    public void GenerateAccessToken_ExpiresAtExpectedTime()
    {
        var manager = CreateManager();
        var user = new AdminUserEntity
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            Role = "Admin"
        };

        var before = DateTimeOffset.UtcNow.AddMinutes(60);
        var (_, expiresAt) = manager.GenerateAccessToken(user);
        var after = DateTimeOffset.UtcNow.AddMinutes(60);

        // ExpiresAt should be within the window
        Assert.InRange(expiresAt, before.AddSeconds(-1), after.AddSeconds(1));
    }

    [Fact]
    public void GenerateAccessToken_NullEmailAndFullName_UsesEmptyString()
    {
        var manager = CreateManager();
        var user = new AdminUserEntity
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            Email = null,
            FullName = null,
            Role = "Admin"
        };

        var (token, _) = manager.GenerateAccessToken(user);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        Assert.Equal("", jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value);
        Assert.Equal("", jwt.Claims.First(c => c.Type == "fullName").Value);
    }

    [Fact]
    public void GenerateAccessToken_CustomExpiration_Respected()
    {
        var config = BuildConfig(new Dictionary<string, string?> { ["Jwt:AccessTokenMinutes"] = "15" });
        var manager = CreateManager(config);
        var user = new AdminUserEntity
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            Role = "Admin"
        };

        var before = DateTimeOffset.UtcNow.AddMinutes(15);
        var (_, expiresAt) = manager.GenerateAccessToken(user);
        var after = DateTimeOffset.UtcNow.AddMinutes(15);

        Assert.InRange(expiresAt, before.AddSeconds(-1), after.AddSeconds(1));
    }

    [Fact]
    public void GenerateAccessToken_UniqueJtiEachCall()
    {
        var manager = CreateManager();
        var user = new AdminUserEntity
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            Role = "Admin"
        };

        var (token1, _) = manager.GenerateAccessToken(user);
        var (token2, _) = manager.GenerateAccessToken(user);

        var handler = new JwtSecurityTokenHandler();
        var jti1 = handler.ReadJwtToken(token1).Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;
        var jti2 = handler.ReadJwtToken(token2).Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;

        Assert.NotEqual(jti1, jti2);
    }

    // ================================================================
    // Constructor / Config Edge Cases
    // ================================================================

    [Fact]
    public void Constructor_MissingSecretKey_Throws()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "test"
                // No SecretKey
            })
            .Build();

        var tenantDb = Substitute.For<ITenantDb>();

        Assert.Throws<InvalidOperationException>(() =>
            new AuthManager(tenantDb, config, NullLogger<AuthManager>.Instance));
    }

    [Fact]
    public void Constructor_ShortSecretKey_Throws()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SecretKey"] = "TooShort"
            })
            .Build();

        var tenantDb = Substitute.For<ITenantDb>();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            new AuthManager(tenantDb, config, NullLogger<AuthManager>.Instance));
        Assert.Contains("at least 32 characters", ex.Message);
    }

    [Fact]
    public void Constructor_DefaultConfigValues_AppliedCorrectly()
    {
        // Only provide SecretKey — all other values should use defaults
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SecretKey"] = "ThisIsATestSecretKeyThatIsAtLeast32BytesLong!!"
            })
            .Build();

        var manager = CreateManager(config);
        var user = new AdminUserEntity
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            Role = "Admin"
        };

        var (token, _) = manager.GenerateAccessToken(user);
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        // Should use default issuer/audience
        Assert.Equal("zentech-biz", jwt.Issuer);
        Assert.Contains("zentech-biz-clients", jwt.Audiences);
    }
}
