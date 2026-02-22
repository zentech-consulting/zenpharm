using System.ComponentModel.DataAnnotations;

namespace Api.Features.Bookings;

public sealed record CreateBookingRequest
{
    [Required]
    public Guid ClientId { get; init; }

    [Required]
    public Guid ServiceId { get; init; }

    public Guid? EmployeeId { get; init; }

    [Required]
    public DateTimeOffset StartTime { get; init; }

    public string? Notes { get; init; }
}

public sealed record UpdateBookingRequest
{
    public Guid? EmployeeId { get; init; }

    [Required]
    public DateTimeOffset StartTime { get; init; }

    [Required, MaxLength(30)]
    public string Status { get; init; } = "confirmed";

    public string? Notes { get; init; }
}

public sealed record BookingDto(
    Guid Id,
    Guid ClientId,
    string ClientName,
    Guid ServiceId,
    string ServiceName,
    Guid? EmployeeId,
    string? EmployeeName,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    string Status,
    string? Notes,
    DateTimeOffset CreatedAt);

public sealed record BookingListResponse(
    IReadOnlyList<BookingDto> Items,
    int TotalCount);

public sealed record AvailableSlotDto(
    DateTimeOffset StartTime,
    DateTimeOffset EndTime);
