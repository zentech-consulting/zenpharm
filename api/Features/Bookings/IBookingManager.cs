namespace Api.Features.Bookings;

public interface IBookingManager
{
    Task<BookingDto> CreateAsync(CreateBookingRequest request, CancellationToken ct = default);
    Task<BookingDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<BookingListResponse> ListAsync(int page, int pageSize, DateOnly? date, Guid? employeeId, CancellationToken ct = default);
    Task<BookingDto?> UpdateAsync(Guid id, UpdateBookingRequest request, CancellationToken ct = default);
    Task<bool> CancelAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<AvailableSlotDto>> GetAvailableSlotsAsync(Guid serviceId, DateOnly date, Guid? employeeId, CancellationToken ct = default);
}
