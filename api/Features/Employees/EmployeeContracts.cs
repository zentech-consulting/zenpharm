using System.ComponentModel.DataAnnotations;

namespace Api.Features.Employees;

public sealed record CreateEmployeeRequest
{
    [Required, MaxLength(100)]
    public string FirstName { get; init; } = "";

    [Required, MaxLength(100)]
    public string LastName { get; init; } = "";

    [MaxLength(200)]
    public string? Email { get; init; }

    [MaxLength(20)]
    public string? Phone { get; init; }

    [Required, MaxLength(50)]
    public string Role { get; init; } = "pharmacy_assistant";

    public bool IsActive { get; init; } = true;
}

public sealed record UpdateEmployeeRequest
{
    [Required, MaxLength(100)]
    public string FirstName { get; init; } = "";

    [Required, MaxLength(100)]
    public string LastName { get; init; } = "";

    [MaxLength(200)]
    public string? Email { get; init; }

    [MaxLength(20)]
    public string? Phone { get; init; }

    [Required, MaxLength(50)]
    public string Role { get; init; } = "pharmacy_assistant";

    public bool IsActive { get; init; } = true;
}

public sealed record EmployeeDto(
    Guid Id,
    string FirstName,
    string LastName,
    string? Email,
    string? Phone,
    string Role,
    bool IsActive,
    DateTimeOffset CreatedAt);

public sealed record EmployeeListResponse(
    IReadOnlyList<EmployeeDto> Items,
    int TotalCount);
