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
}

public sealed record ClientDto(
    Guid Id,
    string FirstName,
    string LastName,
    string? Email,
    string? Phone,
    string? Notes,
    DateTimeOffset CreatedAt);

public sealed record ClientListResponse(
    IReadOnlyList<ClientDto> Items,
    int TotalCount);
