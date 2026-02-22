namespace Api.Features.Schedules;

public interface IScheduleManager
{
    Task<ScheduleDto> CreateAsync(CreateScheduleRequest request, CancellationToken ct = default);
    Task<ScheduleDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ScheduleListResponse> ListAsync(DateOnly? startDate, DateOnly? endDate, Guid? employeeId, CancellationToken ct = default);
    Task<ScheduleDto?> UpdateAsync(Guid id, UpdateScheduleRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<ScheduleDto>> GenerateAsync(GenerateScheduleRequest request, CancellationToken ct = default);
}
