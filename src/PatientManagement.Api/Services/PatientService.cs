using Microsoft.EntityFrameworkCore;

using PatientManagement.Api.Data;
using PatientManagement.Api.Models.Dtos;
using PatientManagement.Api.Models.Entities;

namespace PatientManagement.Api.Services;

public class PatientService(
    AppDbContext dbContext,
    IBlobStorageService blobStorage,
    IEmailService emailService,
    TimeProvider timeProvider,
    ILogger<PatientService> logger) : IPatientService
{
    public async Task<PatientResponse> CreateAsync(CreatePatientRequest request, CancellationToken cancellationToken)
    {
        var photoUrl = await UploadPhotoAsync(request.Photo, cancellationToken);

        var patient = new Patient
        {
            PhotoUrl = photoUrl,
            CreatedAtUtc = timeProvider.GetUtcNow().UtcDateTime
        };
        ApplyDetails(patient, request);
        dbContext.Patients.Add(patient);

        await SaveChangesOrDeleteUploadAsync(photoUrl, cancellationToken);
        await SendPatientCreatedEmailAsync(patient, cancellationToken);

        return patient.ToResponse();
    }

    public async Task<PagedResponse<PatientResponse>> GetPagedAsync(
        int page, int pageSize, CancellationToken cancellationToken)
    {
        var totalCount = await dbContext.Patients.CountAsync(cancellationToken);

        // A stable order is required, otherwise Skip/Take can return overlapping pages.
        var patients = await dbContext.Patients
            .AsNoTracking()
            .OrderBy(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResponse<PatientResponse>
        {
            Items = patients.Select(p => p.ToResponse()).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<PatientDetailsResponse?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var patient = await dbContext.Patients
            .AsNoTracking()
            .Include(p => p.Consultations)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        return patient?.ToDetailsResponse();
    }

    public async Task<PatientResponse?> UpdateAsync(
        int id, UpdatePatientRequest request, CancellationToken cancellationToken)
    {
        var patient = await dbContext.Patients.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (patient is null)
        {
            return null;
        }

        var oldPhotoUrl = patient.PhotoUrl;
        var newPhotoUrl = request.Photo is null ? null : await UploadPhotoAsync(request.Photo, cancellationToken);

        ApplyDetails(patient, request);
        patient.PhotoUrl = newPhotoUrl ?? oldPhotoUrl;

        await SaveChangesOrDeleteUploadAsync(newPhotoUrl, cancellationToken);

        // Only delete the old picture once the new URL is safely saved, so the patient never points to a missing blob.
        // CancellationToken.None: the update is already committed, so cleanup should not be cut short by the client.
        if (newPhotoUrl is not null && oldPhotoUrl is not null)
        {
            await blobStorage.DeleteAsync(oldPhotoUrl, CancellationToken.None);
        }

        return patient.ToResponse();
    }

    private static void ApplyDetails(Patient patient, PatientRequestBase request)
    {
        patient.Name = request.Name;
        patient.Age = request.Age;
        patient.Gender = request.Gender;
        patient.Email = request.Email;
    }

    private async Task<string> UploadPhotoAsync(IFormFile photo, CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(photo.FileName).ToLowerInvariant();

        await using var content = photo.OpenReadStream();
        return await blobStorage.UploadAsync(content, photo.ContentType, extension, cancellationToken);
    }

    private async Task SaveChangesOrDeleteUploadAsync(string? uploadedPhotoUrl, CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch when (uploadedPhotoUrl is not null)
        {
            // The blob was uploaded but no row references it, so remove the orphan before rethrowing.
            // CancellationToken.None: if the request was cancelled, the cleanup must still run.
            await blobStorage.DeleteAsync(uploadedPhotoUrl, CancellationToken.None);
            throw;
        }
    }

    private async Task SendPatientCreatedEmailAsync(Patient patient, CancellationToken cancellationToken)
    {
        try
        {
            await emailService.SendPatientCreatedAsync(patient.Name, patient.Email, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // The patient is already saved; a failed notification must not turn a successful create into an error.
            logger.LogError(ex, "Failed to send the patient created email for patient {PatientId}", patient.Id);
        }
    }
}
