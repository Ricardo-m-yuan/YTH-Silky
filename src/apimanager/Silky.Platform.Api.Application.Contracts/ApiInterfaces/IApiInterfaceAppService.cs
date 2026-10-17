using Silky.Platform.Core.Dtos;
using Silky.Rpc.Routing;

namespace Silky.Platform.Api.Application.Contracts.ApiInterfaces;

/// <summary>
/// 接口管理：采集各微服务暴露的服务条目，并对其进行启停、鉴权、限流等运行时管控。
/// 路由：api/apiinterface/*
/// </summary>
[ServiceRoute]
public interface IApiInterfaceAppService
{
    Task<PagedResult<ApiInterfaceDto>> GetPageAsync(GetApiInterfacePageInput input);

    Task<ApiInterfaceDto> GetAsync(long id);

    /// <summary>更新接口配置，并同步到服务条目使其运行时生效</summary>
    Task<ApiInterfaceDto> UpdateAsync(UpdateApiInterfaceInput input);

    /// <summary>启停接口（同步服务条目的 ProhibitExtranet）</summary>
    Task SetEnabledAsync(SetApiEnabledInput input);

    /// <summary>从服务注册中心重新采集接口清单</summary>
    Task<SyncApiResultDto> SyncAsync(SyncApiInput input);

    /// <summary>按服务名查询该服务下的全部接口</summary>
    Task<IReadOnlyList<ApiInterfaceDto>> GetListByServiceAsync(string serviceName);
}
