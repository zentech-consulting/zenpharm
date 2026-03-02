using Api.Features.Notifications;
using Xunit;

namespace Api.Tests.Notifications;

public class NotificationContractTests
{
    [Fact]
    public void NotificationResult_Ok_CreatesSuccess()
    {
        var result = NotificationResult.Ok();

        Assert.True(result.Success);
        Assert.Null(result.Error);
    }

    [Fact]
    public void NotificationResult_Fail_CreatesFailure()
    {
        var result = NotificationResult.Fail("Client not found");

        Assert.False(result.Success);
        Assert.Equal("Client not found", result.Error);
    }

    [Fact]
    public void NotificationResult_RecordEquality()
    {
        var a = NotificationResult.Ok();
        var b = NotificationResult.Ok();

        Assert.Equal(a, b);
    }

    [Fact]
    public void NotificationResult_Inequality()
    {
        var ok = NotificationResult.Ok();
        var fail = NotificationResult.Fail("error");

        Assert.NotEqual(ok, fail);
    }

    [Fact]
    public void SendReminderRequest_DefaultValues()
    {
        var req = new SendReminderRequest();

        Assert.Equal(Guid.Empty, req.BookingId);
    }

    [Fact]
    public void SendReminderRequest_WithBookingId()
    {
        var id = Guid.NewGuid();
        var req = new SendReminderRequest { BookingId = id };

        Assert.Equal(id, req.BookingId);
    }

    [Fact]
    public void PrescriptionReadyRequest_DefaultValues()
    {
        var req = new PrescriptionReadyRequest();

        Assert.Equal(Guid.Empty, req.ClientId);
        Assert.Equal("", req.Message);
    }

    [Fact]
    public void PrescriptionReadyRequest_WithValues()
    {
        var id = Guid.NewGuid();
        var req = new PrescriptionReadyRequest
        {
            ClientId = id,
            Message = "Your prescription is ready"
        };

        Assert.Equal(id, req.ClientId);
        Assert.Equal("Your prescription is ready", req.Message);
    }
}
