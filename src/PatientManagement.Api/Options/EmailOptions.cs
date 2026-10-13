namespace PatientManagement.Api.Options;

public class EmailOptions
{
    public const string SectionName = "Email";

    public string FromAddress { get; set; } = string.Empty;
}
