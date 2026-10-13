using PatientManagement.Api.Models.Dtos;

namespace PatientManagement.Api.Services;

public interface IConsultationService
{
    /// <summary>Adds a consultation to a patient; returns null when the patient does not exist.</summary>
    Task<ConsultationResponse?> CreateAsync(int patientId, ConsultationRequest request, CancellationToken cancellationToken);

    /// <summary>Returns the patient's consultations oldest first, or null when the patient does not exist.</summary>
    Task<IReadOnlyList<ConsultationResponse>?> GetForPatientAsync(int patientId, CancellationToken cancellationToken);

    /// <summary>Returns the consultation, or null when it does not exist.</summary>
    Task<ConsultationResponse?> GetByIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>Replaces the consultation's details; returns null when the consultation does not exist.</summary>
    Task<ConsultationResponse?> UpdateAsync(int id, ConsultationRequest request, CancellationToken cancellationToken);
}
