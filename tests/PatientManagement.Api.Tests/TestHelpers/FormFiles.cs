using Microsoft.AspNetCore.Http;

namespace PatientManagement.Api.Tests.TestHelpers;

public static class FormFiles
{
    public const int DefaultLength = 16;

    public static IFormFile Create(
        string fileName = "photo.png", string contentType = "image/png", long length = DefaultLength)
    {
        var content = new MemoryStream(new byte[length]);
        return new FormFile(content, 0, length, "Photo", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }
}
