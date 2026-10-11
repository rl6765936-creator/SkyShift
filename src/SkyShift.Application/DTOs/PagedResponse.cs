namespace SkyShift.Application.DTOs;

/// <summary>
/// Pagination envelope for list endpoints. Items is the current page, already paged in the
/// database, and TotalCount is the number of rows matching the filters before paging.
/// </summary>
public class PagedResponse<T>
{
    public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();

    // 1-based, echoes back the effective page actually used (after
    // defaulting/clamping an omitted or out-of-range value).
    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalCount { get; set; }

    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
