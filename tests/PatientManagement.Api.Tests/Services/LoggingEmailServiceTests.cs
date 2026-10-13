using Microsoft.Extensions.Logging.Abstractions;
using PatientManagement.Api.Options;
using PatientManagement.Api.Services;

namespace PatientManagement.Api.Tests.Services;

public class LoggingEmailServiceTests
{
    private readonly LoggingEmailService _service = new(
        Microsoft.Extensions.Options.Options.Create(new EmailOptions { FromAddress = "no-reply@example.com" }),
        NullLogger<LoggingEmailService>.Instance);

    [Theory]
    [InlineData("jane@example.com")]
    [InlineData(null)]
    public async Task SendPatientCreatedAsync_AnyRecipient_CompletesWithoutThrowing(string? recipientEmail)
    {
        // Arrange
        const string patientName = "Jane Doe";

        // Act
        var exception = await Record.ExceptionAsync(
            () => _service.SendPatientCreatedAsync(patientName, recipientEmail, CancellationToken.None));

        // Assert
        Assert.Null(exception);
    }
}
