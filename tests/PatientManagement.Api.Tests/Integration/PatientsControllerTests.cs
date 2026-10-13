using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using PatientManagement.Api.Models.Dtos;
using PatientManagement.Api.Models.Entities;

namespace PatientManagement.Api.Tests.Integration;

public class PatientsControllerTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private const string PatientsUrl = "/api/patients";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task CreatePatient_ValidForm_Returns201WithLocationAndPhotoUrl()
    {
        // Arrange
        using var form = PatientForm("Jane Doe", age: "30", gender: "Female", withPhoto: true);

        // Act
        var response = await _client.PostAsync(PatientsUrl, form);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var patient = await response.Content.ReadFromJsonAsync<PatientResponse>(JsonOptions);
        Assert.Equal("Jane Doe", patient!.Name);
        Assert.Equal(Gender.Female, patient.Gender);
        Assert.Equal(CustomWebApplicationFactory.FakePhotoUrl, patient.PhotoUrl);
        Assert.EndsWith($"{PatientsUrl}/{patient.Id}", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task CreatePatient_MissingPhotoAndInvalidAge_Returns400ValidationProblem()
    {
        // Arrange
        using var form = PatientForm("Jane Doe", age: "200", gender: "Female", withPhoto: false);

        // Act
        var response = await _client.PostAsync(PatientsUrl, form);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(JsonOptions);
        Assert.Contains("Photo", problem!.Errors.Keys);
        Assert.Contains("Age", problem.Errors.Keys);
    }

    [Fact]
    public async Task GetPatients_AfterCreate_ReturnsPageContainingPatient()
    {
        // Arrange
        var created = await CreatePatientAsync("List Patient");

        // Act
        var page = await _client.GetFromJsonAsync<PagedResponse<PatientResponse>>(
            $"{PatientsUrl}?page=1&pageSize={PaginationQuery.MaxPageSize}", JsonOptions);

        // Assert
        Assert.True(page!.TotalCount >= 1);
        Assert.Contains(page.Items, p => p.Id == created.Id);
    }

    [Fact]
    public async Task GetPatients_PageSizeTooLarge_Returns400()
    {
        // Act
        var response = await _client.GetAsync($"{PatientsUrl}?pageSize={PaginationQuery.MaxPageSize + 1}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetPatientById_UnknownId_Returns404ProblemDetails()
    {
        // Act
        var response = await _client.GetAsync($"{PatientsUrl}/999999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task UpdatePatient_ExistingPatient_Returns200WithUpdatedFields()
    {
        // Arrange
        var created = await CreatePatientAsync("Before Update");
        using var form = PatientForm("After Update", age: "45", gender: "Other", withPhoto: false);

        // Act
        var response = await _client.PutAsync($"{PatientsUrl}/{created.Id}", form);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<PatientResponse>(JsonOptions);
        Assert.Equal("After Update", updated!.Name);
        Assert.Equal(45, updated.Age);
        Assert.Equal(Gender.Other, updated.Gender);
        Assert.Equal(created.PhotoUrl, updated.PhotoUrl);
    }

    [Fact]
    public async Task UpdatePatient_UnknownId_Returns404()
    {
        // Arrange
        using var form = PatientForm("Nobody", age: "40", gender: "Male", withPhoto: false);

        // Act
        var response = await _client.PutAsync($"{PatientsUrl}/999999", form);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<PatientResponse> CreatePatientAsync(string name)
    {
        using var form = PatientForm(name, age: "30", gender: "Female", withPhoto: true);
        var response = await _client.PostAsync(PatientsUrl, form);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PatientResponse>(JsonOptions))!;
    }

    private static MultipartFormDataContent PatientForm(string name, string age, string gender, bool withPhoto)
    {
        var form = new MultipartFormDataContent
        {
            { new StringContent(name), "Name" },
            { new StringContent(age), "Age" },
            { new StringContent(gender), "Gender" }
        };

        if (withPhoto)
        {
            var photo = new ByteArrayContent([1, 2, 3, 4]);
            photo.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            form.Add(photo, "Photo", "photo.png");
        }

        return form;
    }
}
