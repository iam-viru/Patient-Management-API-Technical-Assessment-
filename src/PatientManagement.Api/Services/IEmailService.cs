namespace PatientManagement.Api.Services;

public interface IEmailService
{
    Task SendPatientCreatedAsync(string patientName, string? recipientEmail, CancellationToken cancellationToken);
}
