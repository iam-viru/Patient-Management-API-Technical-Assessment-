namespace PatientManagement.Api.Services;

public interface IBlobStorageService
{
    /// <summary>Uploads the content under a generated blob name and returns the blob URL.</summary>
    Task<string> UploadAsync(Stream content, string contentType, string fileExtension, CancellationToken cancellationToken);

    /// <summary>Best-effort delete, used to clean up an orphan blob. Never throws for a missing blob.</summary>
    Task DeleteAsync(string blobUrl, CancellationToken cancellationToken);
}
