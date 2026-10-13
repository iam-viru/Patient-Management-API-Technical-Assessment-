namespace PatientManagement.Api.Models.Dtos;

/// <summary>A consultation as returned by the API.</summary>
public record ConsultationResponse
{
    /// <summary>Consultation identifier.</summary>
    public int Id { get; init; }

    /// <summary>The patient this consultation belongs to.</summary>
    public int PatientId { get; init; }

    /// <summary>When the consultation took place.</summary>
    public DateTime ConsultationDate { get; init; }

    /// <summary>Free-text clinical notes.</summary>
    public string? Notes { get; init; }

    /// <summary>Diagnosis given during the consultation.</summary>
    public string? Diagnosis { get; init; }

    /// <summary>When the consultation was recorded (UTC).</summary>
    public DateTime CreatedAtUtc { get; init; }
}
