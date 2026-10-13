namespace PatientManagement.Api.Models.Entities;

public class Consultation
{
    public int Id { get; set; }

    public int PatientId { get; set; }

    public Patient? Patient { get; set; }

    public DateTime ConsultationDate { get; set; }

    public string? Notes { get; set; }

    public string? Diagnosis { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
