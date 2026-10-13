using System.ComponentModel.DataAnnotations;

namespace PatientManagement.Api.Validation;

/// <summary>
/// Accepts a JPEG, PNG or WebP upload of at most 2 MB (assumption A6).
/// A missing file is valid here, so the same attribute works for required and optional photos.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class AllowedImageAttribute : ValidationAttribute
{
    public const long MaxFileSizeBytes = 2 * BytesPerMegabyte;

    private const long BytesPerMegabyte = 1024 * 1024;

    // Extension and content type must agree, so a renamed or mislabelled file is rejected.
    private static readonly Dictionary<string, string> ContentTypesByExtension = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".webp"] = "image/webp"
    };

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not IFormFile file)
        {
            return ValidationResult.Success;
        }

        var error = GetError(file);
        return error is null
            ? ValidationResult.Success
            : new ValidationResult(error, MemberNames(validationContext));
    }

    private static string? GetError(IFormFile file)
    {
        if (file.Length == 0)
        {
            return "The photo is empty.";
        }

        if (file.Length > MaxFileSizeBytes)
        {
            return $"The photo must be {MaxFileSizeBytes / BytesPerMegabyte} MB or smaller.";
        }

        var extension = Path.GetExtension(file.FileName);
        if (!ContentTypesByExtension.TryGetValue(extension, out var expectedContentType))
        {
            return $"The photo must be one of: {string.Join(", ", ContentTypesByExtension.Keys)}.";
        }

        if (!string.Equals(file.ContentType, expectedContentType, StringComparison.OrdinalIgnoreCase))
        {
            return $"The photo content type must be {expectedContentType} for a {extension} file.";
        }

        return null;
    }

    private static string[]? MemberNames(ValidationContext validationContext) =>
        validationContext.MemberName is null ? null : [validationContext.MemberName];
}
