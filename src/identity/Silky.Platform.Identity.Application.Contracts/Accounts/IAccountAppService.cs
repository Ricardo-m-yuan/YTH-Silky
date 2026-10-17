using Microsoft.AspNetCore.Authorization;
using Silky.Rpc.Routing;

namespace Silky.Platform.Identity.Application.Contracts.Accounts;

/// <summary>
/// 账号服务：登录、令牌刷新、当前用户信息。路由：api/account/*
/// </summary>
[ServiceRoute]
public interface IAccountAppService
{
    /// <summary>登录，返回 JWT。匿名可访问</summary>
    [AllowAnonymous]
    Task<LoginOutput> LoginAsync(LoginInput input);

    /// <summary>刷新令牌。匿名可访问</summary>
    [AllowAnonymous]
    Task<LoginOutput> RefreshAsync(RefreshTokenInput input);

    /// <summary>获取当前登录用户及权限</summary>
    Task<CurrentUserOutput> GetCurrentUserInfoAsync();

    /// <summary>修改当前用户密码</summary>
    Task ChangePasswordAsync(ChangePasswordInput input);

    /// <summary>退出登录（清除权限缓存）</summary>
    Task LogoutAsync();
}
