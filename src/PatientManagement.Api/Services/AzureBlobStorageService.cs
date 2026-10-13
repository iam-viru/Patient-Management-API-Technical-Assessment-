using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;
using PatientManagement.Api.Options;

namespace PatientManagement.Api.Services;

public class AzureBlobStorageService(
    BlobServiceClient blobServiceClient,
    IOptions<BlobStorageOptions> options,
    ILogger<AzureBlobStorageService> logger) : IBlobStorageService
{
    private readonly BlobContainerClient _container =
        blobServiceClient.GetBlobContainerClient(options.Value.ContainerName);

    // Registered as a singleton, so the container is created at most once per app instance
    // instead of on every upload. A race only means a second, harmless CreateIfNotExists call.
    private volatile bool _containerReady;

    public async Task<string> UploadAsync(
        Stream content, string contentType, string fileExtension, CancellationToken cancellationToken)
    {
        await EnsureContainerAsync(cancellationToken);

        // Generated name only: the user's file name is never used, which avoids collisions and path tricks.
        var blobName = $"{Guid.NewGuid()}{fileExtension}";
        var blob = _container.GetBlobClient(blobName);

        var uploadOptions = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
        };
        await blob.UploadAsync(content, uploadOptions, cancellationToken);

        return blob.Uri.ToString();
    }

    public async Task DeleteAsync(string blobUrl, CancellationToken cancellationToken)
    {
        try
        {
            var blobName = new BlobUriBuilder(new Uri(blobUrl)).BlobName;
            await _container.GetBlobClient(blobName).DeleteIfExistsAsync(cancellationToken: cancellationToken);
        }
        catch (Exception ex) when (ex is RequestFailedException or UriFormatException)
        {
            // Cleanup must not hide the original error that triggered it, so log and move on.
            logger.LogWarning(ex, "Failed to delete orphan blob {BlobUrl}", blobUrl);
        }
    }

    private async Task EnsureContainerAsync(CancellationToken cancellationToken)
    {
        if (_containerReady)
        {
            return;
        }

        await _container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);
        _containerReady = true;
    }
}
