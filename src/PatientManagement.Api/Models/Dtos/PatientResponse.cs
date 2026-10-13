using PatientManagement.Api.Models.Entities;

namespace PatientManagement.Api.Models.Dtos;

/// <summary>A patient as returned by the API.</summary>
public record PatientResponse
{
    /// <summary>Patient identifier.</summary>
    public int Id { get; init; }

    /// <summary>Full name of the patient.</summary>
    public required string Name { get; init; }

    /// <summary>Age in years.</summary>
    public int Age { get; init; }

    /// <summary>Male, Female or Other.</summary>
    public Gender Gender { get; init; }

    /// <summary>Optional email address.</summary>
    public string? Email { get; init; }

    /// <summary>URL of the patient picture in Blob Storage.</summary>
    public string? PhotoUrl { get; init; }

    /// <summary>When the patient was registered (UTC).</summary>
    public DateTime CreatedAtUtc { get; init; }
}
