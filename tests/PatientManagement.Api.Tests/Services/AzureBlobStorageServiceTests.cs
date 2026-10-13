using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NSubstitute.Extensions;

using PatientManagement.Api.Options;
using PatientManagement.Api.Services;

namespace PatientManagement.Api.Tests.Services;

public class AzureBlobStorageServiceTests
{
    private const string ContainerName = "patient-photos";
    private const string ContainerUrl = "https://account.blob.core.windows.net/patient-photos";

    private readonly BlobContainerClient _container = Substitute.For<BlobContainerClient>();
    private readonly AzureBlobStorageService _service;

    public AzureBlobStorageServiceTests()
    {
        var serviceClient = Substitute.For<BlobServiceClient>();
        serviceClient.GetBlobContainerClient(ContainerName).Returns(_container);

        // Each requested blob gets a fake client whose Uri reflects the requested name.
        _container.GetBlobClient(Arg.Any<string>()).Returns(call =>
        {
            var blob = Substitute.For<BlobClient>();
            blob.Uri.Returns(new Uri($"{ContainerUrl}/{call.Arg<string>()}"));
            return blob;
        });

        var options = Microsoft.Extensions.Options.Options.Create(
            new BlobStorageOptions { ConnectionString = "unused", ContainerName = ContainerName });

        _service = new AzureBlobStorageService(serviceClient, options, NullLogger<AzureBlobStorageService>.Instance);
    }

    [Fact]
    public async Task UploadAsync_ValidContent_UsesGeneratedNameWithExtension()
    {
        // Arrange
        using var content = new MemoryStream([1, 2, 3]);

        // Act
        var url = await _service.UploadAsync(content, "image/png", ".png", CancellationToken.None);

        // Assert
        var blobName = url[(ContainerUrl.Length + 1)..];
        Assert.EndsWith(".png", blobName);
        Assert.True(Guid.TryParse(Path.GetFileNameWithoutExtension(blobName), out _));
    }

    [Fact]
    public async Task UploadAsync_ValidContent_SetsContentTypeHeader()
    {
        // Arrange
        using var content = new MemoryStream([1, 2, 3]);
        BlobClient? uploadedBlob = null;
        _container.Configure().GetBlobClient(Arg.Any<string>()).Returns(_ =>
        {
            uploadedBlob = Substitute.For<BlobClient>();
            uploadedBlob.Uri.Returns(new Uri($"{ContainerUrl}/blob.jpg"));
            return uploadedBlob;
        });

        // Act
        await _service.UploadAsync(content, "image/jpeg", ".jpg", CancellationToken.None);

        // Assert
        await uploadedBlob!.Received(1).UploadAsync(
            content,
            Arg.Is<BlobUploadOptions>(o => o.HttpHeaders.ContentType == "image/jpeg"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UploadAsync_CalledTwice_CreatesPrivateContainerOnlyOnce()
    {
        // Arrange
        using var first = new MemoryStream([1]);
        using var second = new MemoryStream([2]);

        // Act
        await _service.UploadAsync(first, "image/png", ".png", CancellationToken.None);
        await _service.UploadAsync(second, "image/png", ".png", CancellationToken.None);

        // Assert
        await _container.Received(1).CreateIfNotExistsAsync(
            PublicAccessType.None, cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_BlobUrl_CallsDeleteIfExistsForThatBlob()
    {
        // Arrange
        var blob = Substitute.For<BlobClient>();
        // Configure() stops the default GetBlobClient callback from running while re-stubbing.
        _container.Configure().GetBlobClient("abc.png").Returns(blob);

        // Act
        await _service.DeleteAsync($"{ContainerUrl}/abc.png", CancellationToken.None);

        // Assert
        await blob.Received(1).DeleteIfExistsAsync(
            Arg.Any<DeleteSnapshotsOption>(), Arg.Any<BlobRequestConditions>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_StorageFails_DoesNotThrow()
    {
        // Arrange
        var blob = Substitute.For<BlobClient>();
        blob.DeleteIfExistsAsync(
                Arg.Any<DeleteSnapshotsOption>(), Arg.Any<BlobRequestConditions>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new RequestFailedException("storage unavailable"));
        // Configure() stops the default GetBlobClient callback from running while re-stubbing.
        _container.Configure().GetBlobClient("abc.png").Returns(blob);

        // Act
        var exception = await Record.ExceptionAsync(
            () => _service.DeleteAsync($"{ContainerUrl}/abc.png", CancellationToken.None));

        // Assert
        Assert.Null(exception);
    }
}
