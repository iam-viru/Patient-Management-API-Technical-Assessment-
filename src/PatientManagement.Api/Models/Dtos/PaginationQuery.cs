using System.ComponentModel.DataAnnotations;

namespace PatientManagement.Api.Models.Dtos;

/// <summary>Query string paging parameters. Out-of-range values return 400 rather than being silently adjusted.</summary>
public class PaginationQuery
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 10;
    public const int MaxPageSize = 50;

    /// <summary>1-based page number (default 1).</summary>
    [Range(1, int.MaxValue)]
    public int Page { get; init; } = DefaultPage;

    /// <summary>Items per page, 1 to 50 (default 10).</summary>
    [Range(1, MaxPageSize)]
    public int PageSize { get; init; } = DefaultPageSize;
}
