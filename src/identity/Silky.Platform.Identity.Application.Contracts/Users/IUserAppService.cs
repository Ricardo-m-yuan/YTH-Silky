using Silky.Platform.Core.Dtos;
using Silky.Rpc.Routing;

namespace Silky.Platform.Identity.Application.Contracts.Users;

/// <summary>
/// 用户管理。路由推导：api/user/* （HTTP 方法按方法名前缀推断）
/// </summary>
[ServiceRoute]
public interface IUserAppService
{
    Task<PagedResult<UserDto>> GetPageAsync(GetUserPageInput input);

    Task<UserDto> GetAsync(long id);

    Task<UserDto> CreateAsync(CreateUserInput input);

    Task<UserDto> UpdateAsync(UpdateUserInput input);

    Task DeleteAsync(long id);

    Task ResetPasswordAsync(ResetPasswordInput input);

    Task SetUserRolesAsync(SetUserRolesInput input);

    Task SetUserStatusAsync(SetUserStatusInput input);
}
