using Api.Features.Clients;
using Xunit;

namespace Api.Tests.Clients;

public class ClientContractTests
{
    [Fact]
    public void CreateClientRequest_DefaultValues()
    {
        var req = new CreateClientRequest();

        Assert.Equal("", req.FirstName);
        Assert.Equal("", req.LastName);
        Assert.Null(req.Email);
        Assert.Null(req.Phone);
        Assert.Null(req.Notes);
        Assert.Null(req.DateOfBirth);
        Assert.Null(req.Allergies);
        Assert.Null(req.MedicationNotes);
        Assert.Null(req.Tags);
    }

    [Fact]
    public void UpdateClientRequest_DefaultValues()
    {
        var req = new UpdateClientRequest();

        Assert.Equal("", req.FirstName);
        Assert.Equal("", req.LastName);
        Assert.Null(req.Email);
        Assert.Null(req.Phone);
        Assert.Null(req.Notes);
        Assert.Null(req.DateOfBirth);
        Assert.Null(req.Allergies);
        Assert.Null(req.MedicationNotes);
        Assert.Null(req.Tags);
    }

    [Fact]
    public void ClientDto_RecordEquality()
    {
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var dob = new DateOnly(1990, 5, 15);
        var a = new ClientDto(id, "Alice", "Smith", "alice@example.com", "0400000000", null, dob, "Penicillin", "Blood pressure medication", "VIP", now);
        var b = new ClientDto(id, "Alice", "Smith", "alice@example.com", "0400000000", null, dob, "Penicillin", "Blood pressure medication", "VIP", now);

        Assert.Equal(a, b);
    }

    [Fact]
    public void ClientDto_DifferentId_NotEqual()
    {
        var now = DateTimeOffset.UtcNow;
        var a = new ClientDto(Guid.NewGuid(), "Alice", "Smith", null, null, null, null, null, null, null, now);
        var b = new ClientDto(Guid.NewGuid(), "Alice", "Smith", null, null, null, null, null, null, null, now);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void ClientListResponse_EmptyItems()
    {
        var response = new ClientListResponse([], 0);

        Assert.Empty(response.Items);
        Assert.Equal(0, response.TotalCount);
    }

    [Fact]
    public void ClientListResponse_WithItems()
    {
        var items = new List<ClientDto>
        {
            new(Guid.NewGuid(), "Alice", "Smith", null, null, null, null, null, null, null, DateTimeOffset.UtcNow),
            new(Guid.NewGuid(), "Bob", "Jones", "bob@test.com", null, null, null, null, null, null, DateTimeOffset.UtcNow)
        };

        var response = new ClientListResponse(items, 2);

        Assert.Equal(2, response.Items.Count);
        Assert.Equal(2, response.TotalCount);
    }

    [Fact]
    public void CreateClientRequest_WithPharmacyFields()
    {
        var req = new CreateClientRequest
        {
            FirstName = "Alice",
            LastName = "Smith",
            DateOfBirth = new DateOnly(1985, 3, 20),
            Allergies = "Penicillin, Sulfa",
            MedicationNotes = "Takes metformin daily",
            Tags = "diabetes,regular"
        };

        Assert.Equal(new DateOnly(1985, 3, 20), req.DateOfBirth);
        Assert.Equal("Penicillin, Sulfa", req.Allergies);
        Assert.Equal("Takes metformin daily", req.MedicationNotes);
        Assert.Equal("diabetes,regular", req.Tags);
    }

    [Fact]
    public void UpdateClientRequest_WithPharmacyFields()
    {
        var req = new UpdateClientRequest
        {
            FirstName = "Alice",
            LastName = "Smith",
            DateOfBirth = new DateOnly(1985, 3, 20),
            Allergies = "None known",
            MedicationNotes = "Vitamin D supplements",
            Tags = "wellness"
        };

        Assert.Equal(new DateOnly(1985, 3, 20), req.DateOfBirth);
        Assert.Equal("None known", req.Allergies);
        Assert.Equal("Vitamin D supplements", req.MedicationNotes);
        Assert.Equal("wellness", req.Tags);
    }

    [Fact]
    public void ClientDto_PharmacyFields_Populated()
    {
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var dob = new DateOnly(1975, 12, 1);

        var dto = new ClientDto(id, "John", "Doe", "john@test.com", "0412345678",
            "Regular customer", dob, "Codeine", "Asthma puffers", "asthma,regular", now);

        Assert.Equal(dob, dto.DateOfBirth);
        Assert.Equal("Codeine", dto.Allergies);
        Assert.Equal("Asthma puffers", dto.MedicationNotes);
        Assert.Equal("asthma,regular", dto.Tags);
    }

    [Fact]
    public void ClientDto_PharmacyFields_AllNull()
    {
        var dto = new ClientDto(Guid.NewGuid(), "Jane", "Doe", null, null, null,
            null, null, null, null, DateTimeOffset.UtcNow);

        Assert.Null(dto.DateOfBirth);
        Assert.Null(dto.Allergies);
        Assert.Null(dto.MedicationNotes);
        Assert.Null(dto.Tags);
    }
}
