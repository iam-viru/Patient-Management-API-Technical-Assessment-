using Microsoft.Extensions.Options;

using PatientManagement.Api.Options;

namespace PatientManagement.Api.Services;

// Dummy email sender: writes the email to the log instead of sending it.
// This is the swap point for SMTP or Azure Communication Services; only the DI registration changes.
public class LoggingEmailService(
    IOptions<EmailOptions> options,
    ILogger<LoggingEmailService> logger) : IEmailService
{
    public const string PatientCreatedSubject = "Patient registered";

    public Task SendPatientCreatedAsync(string patientName, string? recipientEmail, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(recipientEmail))
        {
            logger.LogInformation("Dummy email skipped for patient {PatientName}: no recipient address", patientName);
            return Task.CompletedTask;
        }

        logger.LogInformation(
            "Dummy email sent. From: {From}, To: {To}, Subject: {Subject}, Patient: {PatientName}",
            options.Value.FromAddress, recipientEmail, PatientCreatedSubject, patientName);

        return Task.CompletedTask;
    }
}
