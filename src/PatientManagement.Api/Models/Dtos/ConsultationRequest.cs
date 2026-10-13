using System.ComponentModel.DataAnnotations;

using PatientManagement.Api.Data.Configurations;

namespace PatientManagement.Api.Models.Dtos;

/// <summary>JSON body used both to add a consultation and to fully replace one (PUT, assumption A5).</summary>
public class ConsultationRequest
{
    /// <summary>When the consultation took place.</summary>
    // Nullable so [Required] can spot a missing date; a plain DateTime would silently default to 0001-01-01.
    [Required]
    public DateTime? ConsultationDate { get; init; }

    /// <summary>Free-text clinical notes (max 2000 characters).</summary>
    [StringLength(ConsultationConfiguration.NotesMaxLength)]
    public string? Notes { get; init; }

    /// <summary>Diagnosis given during the consultation (max 500 characters).</summary>
    [StringLength(ConsultationConfiguration.DiagnosisMaxLength)]
    public string? Diagnosis { get; init; }
}
