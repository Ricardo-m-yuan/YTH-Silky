using Silky.Platform.Core.Dtos;

namespace Silky.Platform.Api.Application.Contracts.ApiInterfaces;

public class ApiInterfaceDto
{
    public long Id { get; set; }

    /// <summary>服务条目全局唯一 Id</summary>
    public string ServiceEntryId { get; set; }

    /// <summary>服务 Id</summary>
    public string ServiceId { get; set; }

    /// <summary>服务名，如 Silky.Platform.Identity</summary>
    public string ServiceName { get; set; }

    /// <summary>接口方法名</summary>
    public string Method { get; set; }

    /// <summary>HTTP 路由，如 api/user</summary>
    public string WebApi { get; set; }

    /// <summary>GET/POST/PUT/DELETE</summary>
    public string HttpMethod { get; set; }

    public long? GroupId { get; set; }

    public string GroupName { get; set; }

    /// <summary>是否启用（false 时禁止外网访问）</summary>
    public bool IsEnabled { get; set; }

    /// <summary>是否需要鉴权</summary>
    public bool RequireAuth { get; set; }

    /// <summary>访问所需权限码，为空表示登录即可访问</summary>
    public string PermissionCode { get; set; }

    /// <summary>超时（毫秒）</summary>
    public int TimeoutMs { get; set; }

    /// <summary>最大并发数</summary>
    public int MaxConcurrent { get; set; }

    /// <summary>是否启用结果缓存拦截</summary>
    public bool EnableCache { get; set; }

    /// <summary>是否为当前服务本地条目</summary>
    public bool IsLocal { get; set; }

    public string Remark { get; set; }

    public DateTime CreatedTime { get; set; }

    public DateTime? UpdatedTime { get; set; }
}

public class GetApiInterfacePageInput : PagedQueryInput
{
    public string ServiceName { get; set; }

    public long? GroupId { get; set; }

    public bool? IsEnabled { get; set; }

    public string HttpMethod { get; set; }
}

public class UpdateApiInterfaceInput
{
    public long Id { get; set; }

    public long? GroupId { get; set; }

    public bool IsEnabled { get; set; } = true;

    public bool RequireAuth { get; set; } = true;

    public string PermissionCode { get; set; }

    public int TimeoutMs { get; set; } = 5000;

    public int MaxConcurrent { get; set; } = 100;

    public bool EnableCache { get; set; }

    public string Remark { get; set; }
}

public class SetApiEnabledInput
{
    public long Id { get; set; }

    public bool IsEnabled { get; set; }
}

/// <summary>同步入参（保持为复杂类型，使路由推导为 POST）</summary>
public class SyncApiInput
{
    /// <summary>true 时仅采集本地服务条目，false 采集注册中心全部条目</summary>
    public bool OnlyLocal { get; set; }
}

public class SyncApiResultDto
{
    public int Total { get; set; }

    public int Added { get; set; }

    public int Updated { get; set; }

    public int Unchanged { get; set; }
}
