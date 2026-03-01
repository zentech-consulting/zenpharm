using Api.Features.Notifications;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Api.Tests.Notifications;

public class EmailTests
{
    [Fact]
    public void EmailSendResult_Ok()
    {
        var result = EmailSendResult.Ok();

        Assert.True(result.Success);
        Assert.Null(result.Error);
    }

    [Fact]
    public void EmailSendResult_Fail()
    {
        var result = EmailSendResult.Fail("Connection refused");

        Assert.False(result.Success);
        Assert.Equal("Connection refused", result.Error);
    }

    [Fact]
    public async Task DryRunEmailService_ReturnsOk()
    {
        var service = new DryRunEmailService(NullLogger<DryRunEmailService>.Instance);

        var result = await service.SendAsync("user@test.com", "Test", "<p>Hello</p>");

        Assert.True(result.Success);
    }

    [Fact]
    public void EmailSendResult_RecordEquality()
    {
        var a = EmailSendResult.Ok();
        var b = EmailSendResult.Ok();

        Assert.Equal(a, b);
    }
}
