using Silky.Platform.Core.Dtos;
using Silky.Rpc.Routing;
using Silky.Rpc.Runtime.Server;

namespace Silky.Platform.Identity.Application.Contracts.Permissions;

/// <summary>
/// 权限管理。路由：api/permission/*
/// <para>标记 ProhibitExtranet 的方法只暴露为 RPC 端点，不生成 HTTP 路由，供网关等内部调用。</para>
/// </summary>
[ServiceRoute]
public interface IPermissionAppService
{
    Task<PagedResult<PermissionDto>> GetPageAsync(GetPermissionPageInput input);

    Task<IReadOnlyList<PermissionDto>> GetAllAsync();

    Task<IReadOnlyList<PermissionTreeNodeDto>> GetTreeAsync();

    Task<PermissionDto> CreateAsync(CreatePermissionInput input);

    Task<PermissionDto> UpdateAsync(UpdatePermissionInput input);

    Task DeleteAsync(long id);

    /// <summary>获取指定用户拥有的全部权限码（走 RPC，不对外暴露 HTTP）</summary>
    [Governance(ProhibitExtranet = true)]
    Task<IReadOnlyList<string>> GetUserPermissionCodesAsync(long userId);

    /// <summary>判断用户是否拥有某权限（走 RPC，不对外暴露 HTTP）</summary>
    [Governance(ProhibitExtranet = true)]
    Task<bool> CheckUserPermissionAsync(long userId, string permissionCode);
}
