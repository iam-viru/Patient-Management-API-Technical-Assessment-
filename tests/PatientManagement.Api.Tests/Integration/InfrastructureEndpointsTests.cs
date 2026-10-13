using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;

namespace PatientManagement.Api.Tests.Integration;

/// <summary>Covers the plumbing around the controllers: health, Swagger and error responses.</summary>
public class InfrastructureEndpointsTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private const string SwaggerJsonUrl = "/swagger/v1/swagger.json";

    [Fact]
    public async Task GetHealth_DatabaseReachable_Returns200Healthy()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task GetRoot_AnyRequest_RedirectsToSwagger()
    {
        // Arrange
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        // Act
        var response = await client.GetAsync("/");

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/swagger", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task GetSwaggerJson_AnyRequest_DocumentsPatientsWithBinaryPhotoAndStringGender()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(SwaggerJsonUrl);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var paths = document.RootElement.GetProperty("paths");
        Assert.True(paths.TryGetProperty("/api/patients", out var patientsPath));
        Assert.True(paths.TryGetProperty("/api/patients/{patientId}/consultations", out _));

        var formProperties = patientsPath.GetProperty("post").GetProperty("requestBody").GetProperty("content")
            .GetProperty("multipart/form-data").GetProperty("schema").GetProperty("properties");
        var photo = formProperties.GetProperty("Photo");
        Assert.Equal("string", photo.GetProperty("type").GetString());
        Assert.Equal("binary", photo.GetProperty("format").GetString());

        var genderSchemaName = formProperties.GetProperty("Gender").GetProperty("$ref").GetString()!.Split('/')[^1];
        var genderSchema = document.RootElement.GetProperty("components").GetProperty("schemas").GetProperty(genderSchemaName);
        Assert.Equal(["Male", "Female", "Other"], genderSchema.GetProperty("enum").EnumerateArray().Select(e => e.GetString()));
    }

    [Fact]
    public async Task GetUnknownRoute_NoMatchingEndpoint_Returns404ProblemDetails()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/does-not-exist");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(StatusCodes.Status404NotFound, problem!.Status);
    }
}
