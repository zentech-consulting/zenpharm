using Api.Common;

namespace Api.Features.Schedules;

internal sealed class ScheduleManager(
    ITenantDb db,
    ILogger<ScheduleManager> logger) : IScheduleManager
{
    public Task<ScheduleDto> CreateAsync(CreateScheduleRequest request, CancellationToken ct = default)
    {
        throw new NotImplementedException("Schedule module not yet implemented — see Phase 1, Subtask 6");
    }

    public Task<ScheduleDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        throw new NotImplementedException("Schedule module not yet implemented — see Phase 1, Subtask 6");
    }

    public Task<ScheduleListResponse> ListAsync(DateOnly? startDate, DateOnly? endDate, Guid? employeeId, CancellationToken ct = default)
    {
        throw new NotImplementedException("Schedule module not yet implemented — see Phase 1, Subtask 6");
    }

    public Task<ScheduleDto?> UpdateAsync(Guid id, UpdateScheduleRequest request, CancellationToken ct = default)
    {
        throw new NotImplementedException("Schedule module not yet implemented — see Phase 1, Subtask 6");
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        throw new NotImplementedException("Schedule module not yet implemented — see Phase 1, Subtask 6");
    }

    public Task<IReadOnlyList<ScheduleDto>> GenerateAsync(GenerateScheduleRequest request, CancellationToken ct = default)
    {
        throw new NotImplementedException("Schedule module not yet implemented — see Phase 1, Subtask 6");
    }
}
