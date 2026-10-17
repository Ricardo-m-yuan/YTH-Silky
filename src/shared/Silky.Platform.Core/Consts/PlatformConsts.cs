namespace Silky.Platform.Core.Consts;

/// <summary>
/// 平台级常量：缓存键、权限约定、服务名
/// </summary>
public static class PlatformConsts
{
    /// <summary>权限码分隔符：identity.user.create</summary>
    public const string PermissionSeparator = ".";

    /// <summary>用户权限缓存键：platform:userperms:{userId}</summary>
    public const string UserPermissionCacheKey = "platform:userperms:{0}";

    /// <summary>用户权限缓存过期（分钟）</summary>
    public const int UserPermissionCacheMinutes = 30;

    /// <summary>网关 JWT 中承载用户 Id 的 Claim 名</summary>
    public const string ClaimUserId = "userid";

    /// <summary>网关 JWT 中承载用户名的 Claim 名</summary>
    public const string ClaimUserName = "username";

    /// <summary>网关 JWT 中承载角色的 Claim 名（对应 ClaimTypes.Role，会被解析为角色声明）</summary>
    public const string ClaimRoles = "roles";

    /// <summary>超级管理员角色编码，拥有全部权限</summary>
    public const string SuperAdminRoleCode = "superadmin";

    public static class Services
    {
        public const string Identity = "Silky.Platform.Identity";
        public const string Api = "Silky.Platform.Api";
        public const string Job = "Silky.Platform.Job";
        public const string Log = "Silky.Platform.Log";
    }
}
