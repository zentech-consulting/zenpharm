using Api.Common;

namespace Api.Features.Employees;

internal sealed class EmployeeManager(
    ITenantDb db,
    ILogger<EmployeeManager> logger) : IEmployeeManager
{
    public Task<EmployeeDto> CreateAsync(CreateEmployeeRequest request, CancellationToken ct = default)
    {
        throw new NotImplementedException("Employee module not yet implemented — see Phase 1, Subtask 7");
    }

    public Task<EmployeeDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        throw new NotImplementedException("Employee module not yet implemented — see Phase 1, Subtask 7");
    }

    public Task<EmployeeListResponse> ListAsync(int page, int pageSize, string? role, CancellationToken ct = default)
    {
        throw new NotImplementedException("Employee module not yet implemented — see Phase 1, Subtask 7");
    }

    public Task<EmployeeDto?> UpdateAsync(Guid id, UpdateEmployeeRequest request, CancellationToken ct = default)
    {
        throw new NotImplementedException("Employee module not yet implemented — see Phase 1, Subtask 7");
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        throw new NotImplementedException("Employee module not yet implemented — see Phase 1, Subtask 7");
    }
}
