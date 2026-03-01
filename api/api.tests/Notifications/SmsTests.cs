using Api.Features.Notifications;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Api.Tests.Notifications;

public class SmsTests
{
    private static IConfiguration BuildConfig(Dictionary<string, string?> overrides)
    {
        var defaults = new Dictionary<string, string?>
        {
            ["SmsBroadcast:Enabled"] = "true",
            ["SmsBroadcast:DryRun"] = "true",
            ["SmsBroadcast:Username"] = "testuser",
            ["SmsBroadcast:Password"] = "testpass",
            ["SmsBroadcast:From"] = "TestApp"
        };

        foreach (var kv in overrides)
            defaults[kv.Key] = kv.Value;

        return new ConfigurationBuilder()
            .AddInMemoryCollection(defaults)
            .Build();
    }

    [Fact]
    public async Task SendAsync_EmptyPhone_ReturnsFail()
    {
        var cfg = BuildConfig(new());
        var result = await Sms.SendAsync(cfg, NullLogger.Instance, "", "Hello", CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("Phone number", result.Error);
    }

    [Fact]
    public async Task SendAsync_EmptyMessage_ReturnsFail()
    {
        var cfg = BuildConfig(new());
        var result = await Sms.SendAsync(cfg, NullLogger.Instance, "0400000000", "", CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("Message", result.Error);
    }

    [Fact]
    public async Task SendAsync_Disabled_ReturnsSkip()
    {
        var cfg = BuildConfig(new() { ["SmsBroadcast:Enabled"] = "false" });
        var result = await Sms.SendAsync(cfg, NullLogger.Instance, "0400000000", "Hello", CancellationToken.None);

        Assert.False(result.Success);
        Assert.True(result.Skipped);
    }

    [Fact]
    public async Task SendAsync_DryRun_ReturnsOk()
    {
        var cfg = BuildConfig(new());
        var result = await Sms.SendAsync(cfg, NullLogger.Instance, "0400000000", "Hello", CancellationToken.None);

        Assert.True(result.Success);
    }

    [Fact]
    public void NormalisePhone_AustralianMobile()
    {
        Assert.Equal("61400000000", Sms.NormalisePhone("0400000000"));
    }

    [Fact]
    public void NormalisePhone_AlreadyInternational()
    {
        Assert.Equal("61400000000", Sms.NormalisePhone("61400000000"));
    }

    [Fact]
    public void MaskPhone_MasksAllButLastFour()
    {
        Assert.Equal("*******0000", Sms.MaskPhone("61400000000"));
    }

    [Fact]
    public void MaskPhone_ShortNumber()
    {
        Assert.Equal("****", Sms.MaskPhone("123"));
    }
}
