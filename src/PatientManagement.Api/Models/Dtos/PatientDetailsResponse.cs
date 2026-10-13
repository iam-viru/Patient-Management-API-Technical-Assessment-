namespace PatientManagement.Api.Models.Dtos;

/// <summary>A single patient together with their consultations.</summary>
public record PatientDetailsResponse : PatientResponse
{
    /// <summary>The patient's consultations, oldest first.</summary>
    public IReadOnlyList<ConsultationResponse> Consultations { get; init; } = [];
}
