using System.ComponentModel.DataAnnotations;

namespace Api.Features.Schedules;

public sealed record CreateScheduleRequest
{
    [Required]
    public Guid EmployeeId { get; init; }

    [Required]
    public DateOnly Date { get; init; }

    [Required]
    public TimeOnly StartTime { get; init; }

    [Required]
    public TimeOnly EndTime { get; init; }

    [MaxLength(50)]
    public string? Location { get; init; }

    public string? Notes { get; init; }
}

public sealed record UpdateScheduleRequest
{
    [Required]
    public TimeOnly StartTime { get; init; }

    [Required]
    public TimeOnly EndTime { get; init; }

    [MaxLength(50)]
    public string? Location { get; init; }

    public string? Notes { get; init; }
}

public sealed record GenerateScheduleRequest
{
    [Required]
    public DateOnly StartDate { get; init; }

    [Required]
    public DateOnly EndDate { get; init; }

    public IReadOnlyList<Guid>? EmployeeIds { get; init; }
}

public sealed record ScheduleDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeName,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string? Location,
    string? Notes,
    DateTimeOffset CreatedAt);

public sealed record ScheduleListResponse(
    IReadOnlyList<ScheduleDto> Items,
    int TotalCount);
