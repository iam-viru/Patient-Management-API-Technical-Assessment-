using Microsoft.AspNetCore.Mvc;

using PatientManagement.Api.Models.Dtos;
using PatientManagement.Api.Services;

namespace PatientManagement.Api.Controllers;

// No class-level route: consultations are reached both under a patient and on their own, so each action
// declares its full route and all consultation endpoints stay in one controller.
[ApiController]
public class ConsultationsController(IConsultationService consultationService) : ControllerBase
{
    /// <summary>
    /// Adds a consultation to a patient. Not in the brief: added (assumptions A2/A3) so there is consultation data
    /// to fetch and update.
    /// </summary>
    /// <response code="201">The consultation was created.</response>
    /// <response code="400">The body is invalid (missing date, notes or diagnosis too long).</response>
    /// <response code="404">No patient has this id.</response>
    [HttpPost("api/patients/{patientId:int}/consultations")]
    [ProducesResponseType<ConsultationResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ConsultationResponse>> CreateConsultation(
        int patientId, ConsultationRequest request, CancellationToken cancellationToken)
    {
        var consultation = await consultationService.CreateAsync(patientId, request, cancellationToken);
        return consultation is null
            ? PatientNotFound(patientId)
            : CreatedAtAction(nameof(GetConsultationById), new { id = consultation.Id }, consultation);
    }

    /// <summary>Lists a patient's consultations, oldest first.</summary>
    /// <response code="200">The patient's consultations (empty when there are none).</response>
    /// <response code="404">No patient has this id.</response>
    [HttpGet("api/patients/{patientId:int}/consultations")]
    [ProducesResponseType<IReadOnlyList<ConsultationResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ConsultationResponse>>> GetPatientConsultations(
        int patientId, CancellationToken cancellationToken)
    {
        var consultations = await consultationService.GetForPatientAsync(patientId, cancellationToken);
        return consultations is null ? PatientNotFound(patientId) : Ok(consultations);
    }

    /// <summary>Gets one consultation.</summary>
    /// <response code="200">The consultation.</response>
    /// <response code="404">No consultation has this id.</response>
    [HttpGet("api/consultations/{id:int}")]
    [ProducesResponseType<ConsultationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ConsultationResponse>> GetConsultationById(
        int id, CancellationToken cancellationToken)
    {
        var consultation = await consultationService.GetByIdAsync(id, cancellationToken);
        return consultation is null ? ConsultationNotFound(id) : consultation;
    }

    /// <summary>Replaces a consultation's details.</summary>
    /// <response code="200">The updated consultation.</response>
    /// <response code="400">The body is invalid.</response>
    /// <response code="404">No consultation has this id.</response>
    [HttpPut("api/consultations/{id:int}")]
    [ProducesResponseType<ConsultationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ConsultationResponse>> UpdateConsultation(
        int id, ConsultationRequest request, CancellationToken cancellationToken)
    {
        var consultation = await consultationService.UpdateAsync(id, request, cancellationToken);
        return consultation is null ? ConsultationNotFound(id) : consultation;
    }

    private ObjectResult PatientNotFound(int patientId) =>
        Problem(detail: $"Patient {patientId} was not found.", statusCode: StatusCodes.Status404NotFound);

    private ObjectResult ConsultationNotFound(int id) =>
        Problem(detail: $"Consultation {id} was not found.", statusCode: StatusCodes.Status404NotFound);
}
