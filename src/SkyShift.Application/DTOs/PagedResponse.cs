namespace SkyShift.Application.DTOs;

/// <summary>
/// Generic server-side pagination envelope for list endpoints. Items is
/// already the current page's slice -- Skip/Take applied in the database
/// by the repository, not after loading every matching row -- and
/// TotalCount is the count of ALL rows matching the query's filters
/// (pre-paging), so the client can tell how many pages exist and whether
/// it is looking at the last one, without fetching every page first.
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
