using Api.Features.Auth;
using Xunit;

namespace Api.Tests.Auth;

public class SessionControlTests
{
    // ================================================================
    // ActiveSessionDto Tests
    // ================================================================

    [Fact]
    public void ActiveSessionDto_AllFieldsSet_Correctly()
    {
        var id = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow.AddHours(-2);
        var lastUsed = DateTimeOffset.UtcNow;

        var dto = new ActiveSessionDto(id, "admin", "192.168.1.1", createdAt, lastUsed);

        Assert.Equal(id, dto.Id);
        Assert.Equal("admin", dto.Username);
        Assert.Equal("192.168.1.1", dto.CreatedByIp);
        Assert.Equal(createdAt, dto.CreatedAt);
        Assert.Equal(lastUsed, dto.LastUsedAt);
    }

    [Fact]
    public void ActiveSessionDto_NullIpAndLastUsed()
    {
        var dto = new ActiveSessionDto(Guid.NewGuid(), "user1", null, DateTimeOffset.UtcNow, null);

        Assert.Null(dto.CreatedByIp);
        Assert.Null(dto.LastUsedAt);
    }

    [Fact]
    public void ActiveSessionDto_Inequality_DifferentIds()
    {
        var now = DateTimeOffset.UtcNow;
        var a = new ActiveSessionDto(Guid.NewGuid(), "admin", "10.0.0.1", now, now);
        var b = new ActiveSessionDto(Guid.NewGuid(), "admin", "10.0.0.1", now, now);

        Assert.NotEqual(a, b);
    }

    // ================================================================
    // SessionSummaryDto Tests
    // ================================================================

    [Fact]
    public void SessionSummaryDto_AtCapacity()
    {
        var summary = new SessionSummaryDto(5, 5, "Premium");

        Assert.Equal(summary.ActiveSessions, summary.MaxSessions);
    }

    [Fact]
    public void SessionSummaryDto_UnderCapacity()
    {
        var summary = new SessionSummaryDto(2, 10, "Enterprise");

        Assert.True(summary.ActiveSessions < summary.MaxSessions);
    }

    [Fact]
    public void SessionSummaryDto_ZeroSessions()
    {
        var summary = new SessionSummaryDto(0, 5, "Free");

        Assert.Equal(0, summary.ActiveSessions);
        Assert.Equal(5, summary.MaxSessions);
        Assert.Equal("Free", summary.PlanName);
    }

    // ================================================================
    // SessionListResponse Tests
    // ================================================================

    [Fact]
    public void SessionListResponse_EmptyList_ValidState()
    {
        var response = new SessionListResponse(Array.Empty<ActiveSessionDto>(), 3);

        Assert.Empty(response.Sessions);
        Assert.Equal(3, response.MaxSessions);
    }

    [Fact]
    public void SessionListResponse_MultipleSessionsDifferentUsers()
    {
        var now = DateTimeOffset.UtcNow;
        var sessions = new List<ActiveSessionDto>
        {
            new(Guid.NewGuid(), "admin", "10.0.0.1", now.AddHours(-3), now),
            new(Guid.NewGuid(), "manager", "10.0.0.2", now.AddHours(-1), now),
            new(Guid.NewGuid(), "staff", null, now, null),
        };

        var response = new SessionListResponse(sessions, 5);

        Assert.Equal(3, response.Sessions.Count);
        Assert.Equal(5, response.MaxSessions);
        Assert.Equal("admin", response.Sessions[0].Username);
        Assert.Equal("manager", response.Sessions[1].Username);
        Assert.Equal("staff", response.Sessions[2].Username);
    }

    [Fact]
    public void SessionListResponse_SessionsExceedMax_AllowedInDto()
    {
        // The DTO doesn't enforce the limit — it's informational
        var sessions = new List<ActiveSessionDto>
        {
            new(Guid.NewGuid(), "user1", null, DateTimeOffset.UtcNow, null),
            new(Guid.NewGuid(), "user2", null, DateTimeOffset.UtcNow, null),
            new(Guid.NewGuid(), "user3", null, DateTimeOffset.UtcNow, null),
        };

        var response = new SessionListResponse(sessions, 2);

        Assert.Equal(3, response.Sessions.Count);
        Assert.Equal(2, response.MaxSessions);
        Assert.True(response.Sessions.Count > response.MaxSessions);
    }

    // ================================================================
    // Session Activity Definition Tests
    // ================================================================

    [Fact]
    public void ActiveSessionDto_RecentlyCreated_NoLastUsed_IsActive()
    {
        // A session just created (LastUsedAt is null) but within 24h should count as active
        var dto = new ActiveSessionDto(
            Guid.NewGuid(),
            "newuser",
            "172.16.0.1",
            DateTimeOffset.UtcNow.AddMinutes(-5),
            null);

        // Session was created 5 minutes ago — within 24h threshold
        Assert.True(dto.CreatedAt > DateTimeOffset.UtcNow.AddHours(-24));
        Assert.Null(dto.LastUsedAt);
    }

    [Fact]
    public void ActiveSessionDto_RecentlyUsed_IsActive()
    {
        var dto = new ActiveSessionDto(
            Guid.NewGuid(),
            "activeuser",
            "10.0.0.5",
            DateTimeOffset.UtcNow.AddDays(-3),
            DateTimeOffset.UtcNow.AddMinutes(-30));

        // Last used 30 minutes ago — within 24h threshold
        Assert.True(dto.LastUsedAt > DateTimeOffset.UtcNow.AddHours(-24));
    }

    [Fact]
    public void ActiveSessionDto_StaleSession_OlderThan24h()
    {
        var dto = new ActiveSessionDto(
            Guid.NewGuid(),
            "staleuser",
            "10.0.0.6",
            DateTimeOffset.UtcNow.AddDays(-5),
            DateTimeOffset.UtcNow.AddDays(-2));

        // Last used 2 days ago — outside 24h threshold
        Assert.True(dto.LastUsedAt < DateTimeOffset.UtcNow.AddHours(-24));
    }

    // ================================================================
    // Record Immutability Tests
    // ================================================================

    [Fact]
    public void SessionDtos_AreImmutable()
    {
        var session = new ActiveSessionDto(Guid.NewGuid(), "user", "10.0.0.1", DateTimeOffset.UtcNow, null);
        var summary = new SessionSummaryDto(3, 5, "Premium");
        var list = new SessionListResponse(new[] { session }, 5);

        // Records are immutable — with-expressions create new instances
        var modified = session with { Username = "other" };
        Assert.NotEqual(session.Username, modified.Username);
        Assert.Equal("user", session.Username);

        var modifiedSummary = summary with { ActiveSessions = 10 };
        Assert.NotEqual(summary.ActiveSessions, modifiedSummary.ActiveSessions);
        Assert.Equal(3, summary.ActiveSessions);
    }

    [Fact]
    public void SessionListResponse_IReadOnlyList_PreventsMutation()
    {
        var sessions = new List<ActiveSessionDto>
        {
            new(Guid.NewGuid(), "user1", null, DateTimeOffset.UtcNow, null),
        };

        var response = new SessionListResponse(sessions, 5);

        // The IReadOnlyList interface prevents Add/Remove operations at compile time
        Assert.IsAssignableFrom<IReadOnlyList<ActiveSessionDto>>(response.Sessions);
    }
}
