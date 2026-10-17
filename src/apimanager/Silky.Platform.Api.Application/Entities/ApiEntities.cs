using Silky.Platform.Core.Entities;

namespace Silky.Platform.Api.Application.Entities;

/// <summary>接口分组</summary>
public class ApiGroup : EntityBase
{
    public string Code { get; set; }

    public string Name { get; set; }

    public int Sort { get; set; }

    public string Remark { get; set; }
}

/// <summary>
/// 接口条目：由 Silky 服务条目（ServiceEntry）采集而来
/// </summary>
public class ApiInterface : EntityBase
{
    /// <summary>服务条目全局 Id，对应 ServiceEntryDescriptor.Id</summary>
    public string ServiceEntryId { get; set; }

    public string ServiceId { get; set; }

    public string ServiceName { get; set; }

    /// <summary>接口方法名</summary>
    public string Method { get; set; }

    /// <summary>HTTP 路由</summary>
    public string WebApi { get; set; }

    public string HttpMethod { get; set; }

    public long? GroupId { get; set; }

    /// <summary>是否启用（false → 服务条目禁止外网访问）</summary>
    public bool IsEnabled { get; set; } = true;

    public bool RequireAuth { get; set; } = true;

    /// <summary>访问所需权限码</summary>
    public string PermissionCode { get; set; }

    public int TimeoutMs { get; set; } = 5000;

    public int MaxConcurrent { get; set; } = 100;

    public bool EnableCache { get; set; }

    /// <summary>是否为本地条目（本地条目才支持运行时管控即时生效）</summary>
    public bool IsLocal { get; set; }

    public string Remark { get; set; }

    public ApiGroup Group { get; set; }
}
