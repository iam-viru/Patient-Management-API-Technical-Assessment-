namespace PatientManagement.Api.Models.Entities;

public class Patient
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int Age { get; set; }

    public Gender Gender { get; set; }

    public string? Email { get; set; }

    public string? PhotoUrl { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public ICollection<Consultation> Consultations { get; set; } = new List<Consultation>();
}
