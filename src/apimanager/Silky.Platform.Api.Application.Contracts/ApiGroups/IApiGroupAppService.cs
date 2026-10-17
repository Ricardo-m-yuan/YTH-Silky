using Silky.Rpc.Routing;

namespace Silky.Platform.Api.Application.Contracts.ApiGroups;

/// <summary>
/// 接口分组管理。路由：api/apigroup/*
/// </summary>
[ServiceRoute]
public interface IApiGroupAppService
{
    Task<IReadOnlyList<ApiGroupDto>> GetAllAsync();

    Task<ApiGroupDto> GetAsync(long id);

    Task<ApiGroupDto> CreateAsync(CreateApiGroupInput input);

    Task<ApiGroupDto> UpdateAsync(UpdateApiGroupInput input);

    Task DeleteAsync(long id);
}
