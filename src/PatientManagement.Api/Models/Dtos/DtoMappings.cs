using PatientManagement.Api.Models.Entities;

namespace PatientManagement.Api.Models.Dtos;

public static class DtoMappings
{
    public static PatientResponse ToResponse(this Patient patient) => new()
    {
        Id = patient.Id,
        Name = patient.Name,
        Age = patient.Age,
        Gender = patient.Gender,
        Email = patient.Email,
        PhotoUrl = patient.PhotoUrl,
        CreatedAtUtc = patient.CreatedAtUtc
    };

    public static PatientDetailsResponse ToDetailsResponse(this Patient patient) => new()
    {
        Id = patient.Id,
        Name = patient.Name,
        Age = patient.Age,
        Gender = patient.Gender,
        Email = patient.Email,
        PhotoUrl = patient.PhotoUrl,
        CreatedAtUtc = patient.CreatedAtUtc,
        Consultations = patient.Consultations
            .OrderBy(c => c.ConsultationDate)
            .Select(c => c.ToResponse())
            .ToList()
    };

    public static ConsultationResponse ToResponse(this Consultation consultation) => new()
    {
        Id = consultation.Id,
        PatientId = consultation.PatientId,
        ConsultationDate = consultation.ConsultationDate,
        Notes = consultation.Notes,
        Diagnosis = consultation.Diagnosis,
        CreatedAtUtc = consultation.CreatedAtUtc
    };
}
