using Silky.Platform.Core.Dtos;
using Silky.Rpc.Routing;

namespace Silky.Platform.Identity.Application.Contracts.Roles;

/// <summary>
/// 角色管理。路由：api/role/*
/// </summary>
[ServiceRoute]
public interface IRoleAppService
{
    Task<PagedResult<RoleDto>> GetPageAsync(GetRolePageInput input);

    Task<IReadOnlyList<RoleDto>> GetAllAsync();

    Task<RoleDto> GetAsync(long id);

    Task<RoleDto> CreateAsync(CreateRoleInput input);

    Task<RoleDto> UpdateAsync(UpdateRoleInput input);

    Task DeleteAsync(long id);

    /// <summary>为角色分配权限</summary>
    Task SetRolePermissionsAsync(SetRolePermissionsInput input);

    /// <summary>获取角色已持有的权限码</summary>
    Task<IReadOnlyList<string>> GetRolePermissionCodesAsync(long roleId);
}
