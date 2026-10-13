using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.AspNetCore.Mvc;

using PatientManagement.Api.Data.Configurations;
using PatientManagement.Api.Models.Dtos;

namespace PatientManagement.Api.Tests.Integration;

public class ConsultationsControllerTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    private const string ConsultationsUrl = "/api/consultations";
    private const int UnknownId = 999999;

    private static readonly DateTime ConsultationDate = new(2026, 1, 10, 9, 0, 0, DateTimeKind.Utc);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task CreateConsultation_ExistingPatient_Returns201WithLocation()
    {
        // Arrange
        var patientId = await CreatePatientAsync();

        // Act
        var response = await _client.PostAsJsonAsync(PatientConsultationsUrl(patientId), Request("Flu"));

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var consultation = await response.Content.ReadFromJsonAsync<ConsultationResponse>(JsonOptions);
        Assert.Equal(patientId, consultation!.PatientId);
        Assert.Equal("Flu", consultation.Diagnosis);
        Assert.EndsWith($"{ConsultationsUrl}/{consultation.Id}", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task CreateConsultation_UnknownPatient_Returns404()
    {
        // Act
        var response = await _client.PostAsJsonAsync(PatientConsultationsUrl(UnknownId), Request("Flu"));

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetPatientConsultations_AfterCreate_Returns200WithConsultation()
    {
        // Arrange
        var patientId = await CreatePatientAsync();
        var created = await CreateConsultationAsync(patientId);

        // Act
        var consultations = await _client.GetFromJsonAsync<List<ConsultationResponse>>(
            PatientConsultationsUrl(patientId), JsonOptions);

        // Assert
        var consultation = Assert.Single(consultations!);
        Assert.Equal(created.Id, consultation.Id);
    }

    [Fact]
    public async Task GetConsultationById_UnknownId_Returns404ProblemDetails()
    {
        // Act
        var response = await _client.GetAsync($"{ConsultationsUrl}/{UnknownId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task UpdateConsultation_ExistingConsultation_Returns200WithUpdatedFields()
    {
        // Arrange
        var patientId = await CreatePatientAsync();
        var created = await CreateConsultationAsync(patientId);

        // Act
        var response = await _client.PutAsJsonAsync($"{ConsultationsUrl}/{created.Id}", Request("Cold"));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<ConsultationResponse>(JsonOptions);
        Assert.Equal("Cold", updated!.Diagnosis);
    }

    [Fact]
    public async Task UpdateConsultation_MissingDateAndDiagnosisTooLong_Returns400ValidationProblem()
    {
        // Arrange
        var patientId = await CreatePatientAsync();
        var created = await CreateConsultationAsync(patientId);
        var invalid = new { Diagnosis = new string('x', ConsultationConfiguration.DiagnosisMaxLength + 1) };

        // Act
        var response = await _client.PutAsJsonAsync($"{ConsultationsUrl}/{created.Id}", invalid);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(JsonOptions);
        Assert.Contains("ConsultationDate", problem!.Errors.Keys);
        Assert.Contains("Diagnosis", problem.Errors.Keys);
    }

    private static string PatientConsultationsUrl(int patientId) => $"/api/patients/{patientId}/consultations";

    private static ConsultationRequest Request(string diagnosis) => new()
    {
        ConsultationDate = ConsultationDate,
        Notes = "Patient reports symptoms.",
        Diagnosis = diagnosis
    };

    private async Task<ConsultationResponse> CreateConsultationAsync(int patientId)
    {
        var response = await _client.PostAsJsonAsync(PatientConsultationsUrl(patientId), Request("Flu"));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ConsultationResponse>(JsonOptions))!;
    }

    private async Task<int> CreatePatientAsync()
    {
        using var form = new MultipartFormDataContent
        {
            { new StringContent("Consultation Patient"), "Name" },
            { new StringContent("30"), "Age" },
            { new StringContent("Female"), "Gender" }
        };
        var photo = new ByteArrayContent([1, 2, 3, 4]);
        photo.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(photo, "Photo", "photo.png");

        var response = await _client.PostAsync("/api/patients", form);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PatientResponse>(JsonOptions))!.Id;
    }
}
