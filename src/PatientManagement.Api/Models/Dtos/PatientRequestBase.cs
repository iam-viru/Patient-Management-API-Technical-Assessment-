using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Mvc.ModelBinding;

using PatientManagement.Api.Data.Configurations;
using PatientManagement.Api.Models.Entities;

namespace PatientManagement.Api.Models.Dtos;

/// <summary>Patient fields shared by the create and update forms.</summary>
public abstract class PatientRequestBase
{
    public const int MinAge = 0;
    public const int MaxAge = 120;

    /// <summary>Full name of the patient.</summary>
    [Required]
    [StringLength(PatientConfiguration.NameMaxLength)]
    public string Name { get; init; } = string.Empty;

    /// <summary>Age in years (0-120).</summary>
    // [Required] cannot spot a missing int (it silently defaults to 0), so binding itself must fail instead.
    [BindRequired]
    [Range(MinAge, MaxAge)]
    public int Age { get; init; }

    /// <summary>Male, Female or Other.</summary>
    [BindRequired]
    public Gender Gender { get; init; }

    /// <summary>Optional email address that receives the (dummy) welcome email.</summary>
    [EmailAddress]
    [StringLength(PatientConfiguration.EmailMaxLength)]
    public string? Email { get; init; }
}
