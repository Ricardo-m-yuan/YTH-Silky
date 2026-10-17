namespace Silky.Platform.Core.Dtos;

/// <summary>
/// 统一分页返回结构
/// </summary>
public class PagedResult<T>
{
    public long Total { get; set; }

    public int PageIndex { get; set; }

    public int PageSize { get; set; }

    public IReadOnlyList<T> Items { get; set; } = new List<T>();

    public PagedResult()
    {
    }

    public PagedResult(long total, int pageIndex, int pageSize, IReadOnlyList<T> items)
    {
        Total = total;
        PageIndex = pageIndex;
        PageSize = pageSize;
        Items = items;
    }
}
