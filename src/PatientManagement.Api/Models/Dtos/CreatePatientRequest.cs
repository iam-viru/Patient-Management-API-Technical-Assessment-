using System.ComponentModel.DataAnnotations;
using PatientManagement.Api.Validation;

namespace PatientManagement.Api.Models.Dtos;

/// <summary>Multipart form used to register a new patient.</summary>
public class CreatePatientRequest : PatientRequestBase
{
    /// <summary>Patient picture: .jpg, .jpeg, .png or .webp, at most 2 MB.</summary>
    // null! is safe: [Required] rejects the request before any code reads a missing photo.
    [Required]
    [AllowedImage]
    public IFormFile Photo { get; init; } = null!;
}
