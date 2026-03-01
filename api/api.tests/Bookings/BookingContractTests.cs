using Api.Features.Bookings;
using Xunit;

namespace Api.Tests.Bookings;

public class BookingContractTests
{
    [Fact]
    public void CreateBookingRequest_DefaultValues()
    {
        var req = new CreateBookingRequest();

        Assert.Equal(Guid.Empty, req.ClientId);
        Assert.Equal(Guid.Empty, req.ServiceId);
        Assert.Null(req.EmployeeId);
        Assert.Equal(default, req.StartTime);
        Assert.Null(req.Notes);
    }

    [Fact]
    public void UpdateBookingRequest_DefaultValues()
    {
        var req = new UpdateBookingRequest();

        Assert.Null(req.EmployeeId);
        Assert.Equal(default, req.StartTime);
        Assert.Equal("confirmed", req.Status);
        Assert.Null(req.Notes);
    }

    [Fact]
    public void BookingDto_RecordEquality()
    {
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var start = now.AddHours(1);
        var end = start.AddMinutes(30);

        var a = new BookingDto(id, Guid.NewGuid(), "Alice Smith", Guid.NewGuid(), "Haircut",
            null, null, start, end, "pending", null, now);
        var b = new BookingDto(a.Id, a.ClientId, "Alice Smith", a.ServiceId, "Haircut",
            null, null, start, end, "pending", null, now);

        Assert.Equal(a, b);
    }

    [Fact]
    public void BookingDto_DifferentStatus_NotEqual()
    {
        var id = Guid.NewGuid();
        var clientId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var start = now.AddHours(1);
        var end = start.AddMinutes(30);

        var a = new BookingDto(id, clientId, "Alice", serviceId, "Haircut",
            null, null, start, end, "pending", null, now);
        var b = new BookingDto(id, clientId, "Alice", serviceId, "Haircut",
            null, null, start, end, "confirmed", null, now);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void BookingListResponse_EmptyItems()
    {
        var response = new BookingListResponse([], 0);

        Assert.Empty(response.Items);
        Assert.Equal(0, response.TotalCount);
    }

    [Fact]
    public void BookingListResponse_WithItems()
    {
        var now = DateTimeOffset.UtcNow;
        var items = new List<BookingDto>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), "Alice", Guid.NewGuid(), "Haircut",
                null, null, now, now.AddMinutes(30), "pending", null, now)
        };

        var response = new BookingListResponse(items, 1);

        Assert.Single(response.Items);
        Assert.Equal(1, response.TotalCount);
    }

    [Fact]
    public void AvailableSlotDto_RecordEquality()
    {
        var start = DateTimeOffset.UtcNow;
        var end = start.AddMinutes(30);

        var a = new AvailableSlotDto(start, end);
        var b = new AvailableSlotDto(start, end);

        Assert.Equal(a, b);
    }

    [Fact]
    public void AvailableSlotDto_DifferentTimes_NotEqual()
    {
        var start = DateTimeOffset.UtcNow;

        var a = new AvailableSlotDto(start, start.AddMinutes(30));
        var b = new AvailableSlotDto(start, start.AddMinutes(60));

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void BookingDto_WithEmployeeName()
    {
        var empId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var dto = new BookingDto(Guid.NewGuid(), Guid.NewGuid(), "Client Name",
            Guid.NewGuid(), "Service Name", empId, "Jane Doe",
            now, now.AddMinutes(30), "confirmed", "Some notes", now);

        Assert.Equal(empId, dto.EmployeeId);
        Assert.Equal("Jane Doe", dto.EmployeeName);
        Assert.Equal("Some notes", dto.Notes);
    }
}
