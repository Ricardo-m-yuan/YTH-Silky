using Microsoft.EntityFrameworkCore;
using Silky.Platform.Core.Consts;
using Silky.Platform.Identity.Application.EntityFrameworkCore;
using Silky.Platform.Identity.Application.Entities;

namespace Silky.Platform.Identity.Application.Permissions;

/// <summary>
/// 用户权限解析器：贯穿登录、鉴权、权限缓存的唯一实现
/// </summary>
public class UserPermissionResolver
{
    private readonly IdentityDbContext _db;

    public UserPermissionResolver(IdentityDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<string>> GetRoleCodesAsync(long userId)
    {
        return await (from ur in _db.UserRoles.AsNoTracking()
                join r in _db.Roles.AsNoTracking() on ur.RoleId equals r.Id
                where ur.UserId == userId && !r.IsDeleted && r.Status == 1
                select r.Code)
            .ToListAsync();
    }

    /// <summary>
    /// 取用户全部权限码。超级管理员角色返回全部权限。
    /// </summary>
    public async Task<IReadOnlyList<string>> GetPermissionCodesAsync(long userId)
    {
        var roleCodes = await GetRoleCodesAsync(userId);

        if (roleCodes.Contains(PlatformConsts.SuperAdminRoleCode))
        {
            return await _db.Permissions.AsNoTracking()
                .Where(p => !p.IsDeleted)
                .Select(p => p.Code)
                .ToListAsync();
        }

        return await (from ur in _db.UserRoles.AsNoTracking()
                join r in _db.Roles.AsNoTracking() on ur.RoleId equals r.Id
                join rp in _db.RolePermissions.AsNoTracking() on r.Id equals rp.RoleId
                join p in _db.Permissions.AsNoTracking() on rp.PermissionId equals p.Id
                where ur.UserId == userId && !r.IsDeleted && r.Status == 1 && !p.IsDeleted
                select p.Code)
            .Distinct()
            .ToListAsync();
    }
}
