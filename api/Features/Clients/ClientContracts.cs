using System.ComponentModel.DataAnnotations;

namespace Api.Features.Clients;

public sealed record CreateClientRequest
{
    [Required, MaxLength(100)]
    public string FirstName { get; init; } = "";

    [Required, MaxLength(100)]
    public string LastName { get; init; } = "";

    [MaxLength(200)]
    public string? Email { get; init; }

    [MaxLength(20)]
    public string? Phone { get; init; }

    [MaxLength(2000)]
    public string? Notes { get; init; }

    public DateOnly? DateOfBirth { get; init; }

    [MaxLength(2000)]
    public string? Allergies { get; init; }

    [MaxLength(2000)]
    public string? MedicationNotes { get; init; }

    [MaxLength(500)]
    public string? Tags { get; init; }
}

public sealed record UpdateClientRequest
{
    [Required, MaxLength(100)]
    public string FirstName { get; init; } = "";

    [Required, MaxLength(100)]
    public string LastName { get; init; } = "";

    [MaxLength(200)]
    public string? Email { get; init; }

    [MaxLength(20)]
    public string? Phone { get; init; }

    [MaxLength(2000)]
    public string? Notes { get; init; }

    public DateOnly? DateOfBirth { get; init; }

    [MaxLength(2000)]
    public string? Allergies { get; init; }

    [MaxLength(2000)]
    public string? MedicationNotes { get; init; }

    [MaxLength(500)]
    public string? Tags { get; init; }
}

public sealed record ClientDto(
    Guid Id,
    string FirstName,
    string LastName,
    string? Email,
    string? Phone,
    string? Notes,
    DateOnly? DateOfBirth,
    string? Allergies,
    string? MedicationNotes,
    string? Tags,
    DateTimeOffset CreatedAt);

public sealed record ClientListResponse(
    IReadOnlyList<ClientDto> Items,
    int TotalCount);
