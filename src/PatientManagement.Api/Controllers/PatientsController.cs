using Microsoft.AspNetCore.Mvc;

using PatientManagement.Api.Models.Dtos;
using PatientManagement.Api.Services;

namespace PatientManagement.Api.Controllers;

[ApiController]
[Route("api/patients")]
public class PatientsController(IPatientService patientService) : ControllerBase
{
    /// <summary>Registers a patient, uploads their picture to Blob Storage and sends a dummy email.</summary>
    /// <response code="201">The patient was created.</response>
    /// <response code="400">The form is invalid (missing fields, out-of-range age, unsupported picture).</response>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType<PatientResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PatientResponse>> CreatePatient(
        [FromForm] CreatePatientRequest request, CancellationToken cancellationToken)
    {
        var patient = await patientService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetPatientById), new { id = patient.Id }, patient);
    }

    /// <summary>Lists patients one page at a time, ordered by id.</summary>
    /// <response code="200">The requested page of patients.</response>
    /// <response code="400">The page or page size is out of range.</response>
    [HttpGet]
    [ProducesResponseType<PagedResponse<PatientResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResponse<PatientResponse>>> GetPatients(
        [FromQuery] PaginationQuery query, CancellationToken cancellationToken)
    {
        return await patientService.GetPagedAsync(query.Page, query.PageSize, cancellationToken);
    }

    /// <summary>Gets one patient together with their consultations.</summary>
    /// <response code="200">The patient.</response>
    /// <response code="404">No patient has this id.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType<PatientDetailsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PatientDetailsResponse>> GetPatientById(int id, CancellationToken cancellationToken)
    {
        var patient = await patientService.GetByIdAsync(id, cancellationToken);
        return patient is null ? PatientNotFound(id) : patient;
    }

    /// <summary>Replaces a patient's details. The picture is optional; when omitted the current one is kept.</summary>
    /// <response code="200">The updated patient.</response>
    /// <response code="400">The form is invalid.</response>
    /// <response code="404">No patient has this id.</response>
    [HttpPut("{id:int}")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType<PatientResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PatientResponse>> UpdatePatient(
        int id, [FromForm] UpdatePatientRequest request, CancellationToken cancellationToken)
    {
        var patient = await patientService.UpdateAsync(id, request, cancellationToken);
        return patient is null ? PatientNotFound(id) : patient;
    }

    private ObjectResult PatientNotFound(int id) =>
        Problem(detail: $"Patient {id} was not found.", statusCode: StatusCodes.Status404NotFound);
}
