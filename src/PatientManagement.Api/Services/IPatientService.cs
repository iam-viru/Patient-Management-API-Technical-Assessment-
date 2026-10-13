using PatientManagement.Api.Models.Dtos;

namespace PatientManagement.Api.Services;

public interface IPatientService
{
    Task<PatientResponse> CreateAsync(CreatePatientRequest request, CancellationToken cancellationToken);

    Task<PagedResponse<PatientResponse>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken);

    /// <summary>Returns the patient with their consultations, or null when the patient does not exist.</summary>
    Task<PatientDetailsResponse?> GetByIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>Replaces the patient's details; returns null when the patient does not exist.</summary>
    Task<PatientResponse?> UpdateAsync(int id, UpdatePatientRequest request, CancellationToken cancellationToken);
}
