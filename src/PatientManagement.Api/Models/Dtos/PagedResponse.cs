namespace PatientManagement.Api.Models.Dtos;

/// <summary>One page of results plus the information needed to request the others.</summary>
public record PagedResponse<T>
{
    /// <summary>Items on this page.</summary>
    public IReadOnlyList<T> Items { get; init; } = [];

    /// <summary>1-based page number.</summary>
    public int Page { get; init; }

    /// <summary>Maximum number of items per page.</summary>
    public int PageSize { get; init; }

    /// <summary>Total number of items across all pages.</summary>
    public int TotalCount { get; init; }
}
