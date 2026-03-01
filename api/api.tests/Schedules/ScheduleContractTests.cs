using Api.Features.Schedules;
using Xunit;

namespace Api.Tests.Schedules;

public class ScheduleContractTests
{
    [Fact]
    public void CreateScheduleRequest_DefaultValues()
    {
        var req = new CreateScheduleRequest();

        Assert.Equal(Guid.Empty, req.EmployeeId);
        Assert.Equal(default, req.Date);
        Assert.Equal(default, req.StartTime);
        Assert.Equal(default, req.EndTime);
        Assert.Null(req.Location);
        Assert.Null(req.Notes);
    }

    [Fact]
    public void UpdateScheduleRequest_DefaultValues()
    {
        var req = new UpdateScheduleRequest();

        Assert.Equal(default, req.StartTime);
        Assert.Equal(default, req.EndTime);
        Assert.Null(req.Location);
        Assert.Null(req.Notes);
    }

    [Fact]
    public void GenerateScheduleRequest_DefaultValues()
    {
        var req = new GenerateScheduleRequest();

        Assert.Equal(default, req.StartDate);
        Assert.Equal(default, req.EndDate);
        Assert.Null(req.EmployeeIds);
    }

    [Fact]
    public void ScheduleDto_RecordEquality()
    {
        var id = Guid.NewGuid();
        var empId = Guid.NewGuid();
        var date = new DateOnly(2025, 6, 15);
        var start = new TimeOnly(9, 0);
        var end = new TimeOnly(17, 0);
        var now = DateTimeOffset.UtcNow;

        var a = new ScheduleDto(id, empId, "Jane Doe", date, start, end, "Room A", null, now);
        var b = new ScheduleDto(id, empId, "Jane Doe", date, start, end, "Room A", null, now);

        Assert.Equal(a, b);
    }

    [Fact]
    public void ScheduleDto_DifferentDate_NotEqual()
    {
        var id = Guid.NewGuid();
        var empId = Guid.NewGuid();
        var start = new TimeOnly(9, 0);
        var end = new TimeOnly(17, 0);
        var now = DateTimeOffset.UtcNow;

        var a = new ScheduleDto(id, empId, "Jane Doe", new DateOnly(2025, 6, 15), start, end, null, null, now);
        var b = new ScheduleDto(id, empId, "Jane Doe", new DateOnly(2025, 6, 16), start, end, null, null, now);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void ScheduleListResponse_EmptyItems()
    {
        var response = new ScheduleListResponse([], 0);

        Assert.Empty(response.Items);
        Assert.Equal(0, response.TotalCount);
    }

    [Fact]
    public void ScheduleListResponse_WithItems()
    {
        var now = DateTimeOffset.UtcNow;
        var items = new List<ScheduleDto>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), "Jane Doe", new DateOnly(2025, 6, 15),
                new TimeOnly(9, 0), new TimeOnly(17, 0), "Room A", null, now),
            new(Guid.NewGuid(), Guid.NewGuid(), "John Smith", new DateOnly(2025, 6, 15),
                new TimeOnly(9, 0), new TimeOnly(17, 0), null, "Morning shift", now)
        };

        var response = new ScheduleListResponse(items, 2);

        Assert.Equal(2, response.Items.Count);
        Assert.Equal(2, response.TotalCount);
    }
}
