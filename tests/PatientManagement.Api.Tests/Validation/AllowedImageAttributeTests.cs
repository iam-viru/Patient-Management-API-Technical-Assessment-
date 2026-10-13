using System.ComponentModel.DataAnnotations;

using PatientManagement.Api.Tests.TestHelpers;
using PatientManagement.Api.Validation;

namespace PatientManagement.Api.Tests.Validation;

public class AllowedImageAttributeTests
{
    private readonly AllowedImageAttribute _attribute = new();
    private readonly ValidationContext _context = new(new object()) { MemberName = "Photo" };

    [Theory]
    [InlineData("photo.jpg", "image/jpeg")]
    [InlineData("photo.JPEG", "image/jpeg")]
    [InlineData("photo.png", "image/png")]
    [InlineData("photo.webp", "image/webp")]
    public void GetValidationResult_SupportedImage_ReturnsSuccess(string fileName, string contentType)
    {
        // Arrange
        var file = FormFiles.Create(fileName, contentType);

        // Act
        var result = _attribute.GetValidationResult(file, _context);

        // Assert
        Assert.Equal(ValidationResult.Success, result);
    }

    [Fact]
    public void GetValidationResult_NoFile_ReturnsSuccess()
    {
        // Act
        var result = _attribute.GetValidationResult(null, _context);

        // Assert
        Assert.Equal(ValidationResult.Success, result);
    }

    [Theory]
    [InlineData("photo.gif", "image/gif")]
    [InlineData("photo.exe", "application/octet-stream")]
    [InlineData("photo", "image/png")]
    public void GetValidationResult_UnsupportedExtension_ReturnsError(string fileName, string contentType)
    {
        // Arrange
        var file = FormFiles.Create(fileName, contentType);

        // Act
        var result = _attribute.GetValidationResult(file, _context);

        // Assert
        Assert.NotEqual(ValidationResult.Success, result);
        Assert.Contains("Photo", result!.MemberNames);
    }

    [Fact]
    public void GetValidationResult_ContentTypeDoesNotMatchExtension_ReturnsError()
    {
        // Arrange
        var file = FormFiles.Create("photo.png", "image/jpeg");

        // Act
        var result = _attribute.GetValidationResult(file, _context);

        // Assert
        Assert.NotEqual(ValidationResult.Success, result);
    }

    [Fact]
    public void GetValidationResult_FileTooLarge_ReturnsError()
    {
        // Arrange
        var file = FormFiles.Create(length: AllowedImageAttribute.MaxFileSizeBytes + 1);

        // Act
        var result = _attribute.GetValidationResult(file, _context);

        // Assert
        Assert.NotEqual(ValidationResult.Success, result);
    }

    [Fact]
    public void GetValidationResult_FileAtSizeLimit_ReturnsSuccess()
    {
        // Arrange
        var file = FormFiles.Create(length: AllowedImageAttribute.MaxFileSizeBytes);

        // Act
        var result = _attribute.GetValidationResult(file, _context);

        // Assert
        Assert.Equal(ValidationResult.Success, result);
    }

    [Fact]
    public void GetValidationResult_EmptyFile_ReturnsError()
    {
        // Arrange
        var file = FormFiles.Create(length: 0);

        // Act
        var result = _attribute.GetValidationResult(file, _context);

        // Assert
        Assert.NotEqual(ValidationResult.Success, result);
    }
}
