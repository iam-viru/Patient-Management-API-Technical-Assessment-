using PatientManagement.Api.Validation;

namespace PatientManagement.Api.Models.Dtos;

/// <summary>Multipart form used to fully replace a patient's details (PUT, assumption A5).</summary>
public class UpdatePatientRequest : PatientRequestBase
{
    /// <summary>Optional new picture (.jpg, .jpeg, .png or .webp, at most 2 MB); when omitted the current picture is kept.</summary>
    [AllowedImage]
    public IFormFile? Photo { get; init; }
}
