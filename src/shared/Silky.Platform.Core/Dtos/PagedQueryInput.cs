namespace Silky.Platform.Core.Dtos;

/// <summary>
/// 统一分页查询入参
/// </summary>
public class PagedQueryInput
{
    public int PageIndex { get; set; } = 1;

    public int PageSize { get; set; } = 20;

    public string Keyword { get; set; }
}
