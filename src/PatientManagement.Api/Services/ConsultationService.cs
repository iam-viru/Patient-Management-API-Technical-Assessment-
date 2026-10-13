using Microsoft.EntityFrameworkCore;

using PatientManagement.Api.Data;
using PatientManagement.Api.Models.Dtos;
using PatientManagement.Api.Models.Entities;

namespace PatientManagement.Api.Services;

public class ConsultationService(AppDbContext dbContext, TimeProvider timeProvider) : IConsultationService
{
    public async Task<ConsultationResponse?> CreateAsync(
        int patientId, ConsultationRequest request, CancellationToken cancellationToken)
    {
        if (!await PatientExistsAsync(patientId, cancellationToken))
        {
            return null;
        }

        var consultation = new Consultation
        {
            PatientId = patientId,
            CreatedAtUtc = timeProvider.GetUtcNow().UtcDateTime
        };
        ApplyDetails(consultation, request);
        dbContext.Consultations.Add(consultation);

        await dbContext.SaveChangesAsync(cancellationToken);

        return consultation.ToResponse();
    }

    public async Task<IReadOnlyList<ConsultationResponse>?> GetForPatientAsync(
        int patientId, CancellationToken cancellationToken)
    {
        // Checked separately so "unknown patient" (404) is not confused with "no consultations yet" (empty list).
        if (!await PatientExistsAsync(patientId, cancellationToken))
        {
            return null;
        }

        var consultations = await dbContext.Consultations
            .AsNoTracking()
            .Where(c => c.PatientId == patientId)
            .OrderBy(c => c.ConsultationDate)
            .ToListAsync(cancellationToken);

        return consultations.Select(c => c.ToResponse()).ToList();
    }

    public async Task<ConsultationResponse?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var consultation = await dbContext.Consultations
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        return consultation?.ToResponse();
    }

    public async Task<ConsultationResponse?> UpdateAsync(
        int id, ConsultationRequest request, CancellationToken cancellationToken)
    {
        var consultation = await dbContext.Consultations.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (consultation is null)
        {
            return null;
        }

        ApplyDetails(consultation, request);
        await dbContext.SaveChangesAsync(cancellationToken);

        return consultation.ToResponse();
    }

    private Task<bool> PatientExistsAsync(int patientId, CancellationToken cancellationToken) =>
        dbContext.Patients.AnyAsync(p => p.Id == patientId, cancellationToken);

    private static void ApplyDetails(Consultation consultation, ConsultationRequest request)
    {
        // [Required] on the DTO rejects a missing date before the service is ever called.
        consultation.ConsultationDate = request.ConsultationDate!.Value;
        consultation.Notes = request.Notes;
        consultation.Diagnosis = request.Diagnosis;
    }
}
