using Api.Common;

namespace Api.Features.Bookings;

internal sealed class BookingManager(
    ITenantDb db,
    ILogger<BookingManager> logger) : IBookingManager
{
    public Task<BookingDto> CreateAsync(CreateBookingRequest request, CancellationToken ct = default)
    {
        throw new NotImplementedException("Booking module not yet implemented — see Phase 1, Subtask 5");
    }

    public Task<BookingDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        throw new NotImplementedException("Booking module not yet implemented — see Phase 1, Subtask 5");
    }

    public Task<BookingListResponse> ListAsync(int page, int pageSize, DateOnly? date, Guid? employeeId, CancellationToken ct = default)
    {
        throw new NotImplementedException("Booking module not yet implemented — see Phase 1, Subtask 5");
    }

    public Task<BookingDto?> UpdateAsync(Guid id, UpdateBookingRequest request, CancellationToken ct = default)
    {
        throw new NotImplementedException("Booking module not yet implemented — see Phase 1, Subtask 5");
    }

    public Task<bool> CancelAsync(Guid id, CancellationToken ct = default)
    {
        throw new NotImplementedException("Booking module not yet implemented — see Phase 1, Subtask 5");
    }

    public Task<IReadOnlyList<AvailableSlotDto>> GetAvailableSlotsAsync(Guid serviceId, DateOnly date, Guid? employeeId, CancellationToken ct = default)
    {
        throw new NotImplementedException("Booking module not yet implemented — see Phase 1, Subtask 5");
    }
}
