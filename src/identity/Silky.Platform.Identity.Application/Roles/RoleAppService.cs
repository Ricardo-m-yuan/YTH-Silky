using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Silky.Core.Exceptions;
using Silky.Platform.Core.Consts;
using Silky.Platform.Core.Dtos;
using Silky.Platform.Identity.Application.Contracts.Roles;
using Silky.Platform.Identity.Application.EntityFrameworkCore;
using Silky.Platform.Identity.Application.Entities;
using Silky.Platform.Identity.Application.Security;

namespace Silky.Platform.Identity.Application.Roles;

public class RoleAppService : IRoleAppService
{
    private readonly IdentityDbContext _db;
    private readonly IDistributedCache _cache;

    public RoleAppService(IdentityDbContext db, IDistributedCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<PagedResult<RoleDto>> GetPageAsync(GetRolePageInput input)
    {
        var query = _db.Roles.AsNoTracking().Where(r => !r.IsDeleted);

        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            query = query.Where(r => r.Code.Contains(input.Keyword) || r.Name.Contains(input.Keyword));
        }

        if (input.Status.HasValue)
        {
            query = query.Where(r => r.Status == input.Status.Value);
        }

        var total = await query.LongCountAsync();
        var roles = await query
            .OrderByDescending(r => r.Id)
            .Skip((input.PageIndex - 1) * input.PageSize)
            .Take(input.PageSize)
            .ToListAsync();

        return new PagedResult<RoleDto>(total, input.PageIndex, input.PageSize,
            roles.Select(Map).ToList());
    }

    public async Task<IReadOnlyList<RoleDto>> GetAllAsync()
    {
        var roles = await _db.Roles.AsNoTracking()
            .Where(r => !r.IsDeleted && r.Status == 1)
            .OrderBy(r => r.Id)
            .ToListAsync();

        return roles.Select(Map).ToList();
    }

    public async Task<RoleDto> GetAsync(long id)
    {
        var role = await _db.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);
        if (role == null)
        {
            throw new BusinessException("角色不存在");
        }

        return Map(role);
    }

    public async Task<RoleDto> CreateAsync(CreateRoleInput input)
    {
        if (await _db.Roles.AnyAsync(r => r.Code == input.Code && !r.IsDeleted))
        {
            throw new BusinessException($"角色编码 {input.Code} 已存在");
        }

        var role = new Role
        {
            Code = input.Code,
            Name = input.Name,
            Remark = input.Remark,
            Status = 1,
            CreatedTime = DateTime.Now,
            CreatorId = CurrentUserAccessor.GetUserId()
        };

        _db.Roles.Add(role);
        await _db.SaveChangesAsync();

        if (input.PermissionIds != null && input.PermissionIds.Count > 0)
        {
            await SetPermissionsInternalAsync(role.Id, input.PermissionIds);
        }

        return Map(role);
    }

    public async Task<RoleDto> UpdateAsync(UpdateRoleInput input)
    {
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Id == input.Id && !r.IsDeleted);
        if (role == null)
        {
            throw new BusinessException("角色不存在");
        }

        role.Name = input.Name;
        role.Remark = input.Remark;
        role.Status = input.Status;
        role.UpdatedTime = DateTime.Now;
        role.UpdaterId = CurrentUserAccessor.GetUserId();

        await _db.SaveChangesAsync();
        await SetPermissionsInternalAsync(role.Id, input.PermissionIds);

        return Map(role);
    }

    public async Task DeleteAsync(long id)
    {
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);
        if (role == null)
        {
            throw new BusinessException("角色不存在");
        }

        if (await _db.UserRoles.AnyAsync(ur => ur.RoleId == id))
        {
            throw new BusinessException("该角色下仍有关联用户，不能删除");
        }

        role.IsDeleted = true;
        await _db.SaveChangesAsync();

        await ClearUsersCacheByRoleAsync(id);
    }

    public async Task SetRolePermissionsAsync(SetRolePermissionsInput input)
    {
        await SetPermissionsInternalAsync(input.RoleId, input.PermissionIds);
    }

    public async Task<IReadOnlyList<string>> GetRolePermissionCodesAsync(long roleId)
    {
        return await (from rp in _db.RolePermissions.AsNoTracking()
                join p in _db.Permissions.AsNoTracking() on rp.PermissionId equals p.Id
                where rp.RoleId == roleId && !p.IsDeleted
                select p.Code)
            .ToListAsync();
    }

    private async Task SetPermissionsInternalAsync(long roleId, IReadOnlyList<long> permissionIds)
    {
        var exists = await _db.RolePermissions.Where(rp => rp.RoleId == roleId).ToListAsync();
        _db.RolePermissions.RemoveRange(exists);

        if (permissionIds != null && permissionIds.Count > 0)
        {
            foreach (var permissionId in permissionIds.Distinct())
            {
                _db.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionId = permissionId });
            }
        }

        await _db.SaveChangesAsync();
        await ClearUsersCacheByRoleAsync(roleId);
    }

    /// <summary>角色权限变更后，清理该角色下所有用户的权限缓存</summary>
    private async Task ClearUsersCacheByRoleAsync(long roleId)
    {
        var userIds = await _db.UserRoles.AsNoTracking()
            .Where(ur => ur.RoleId == roleId)
            .Select(ur => ur.UserId)
            .ToListAsync();

        foreach (var userId in userIds.Distinct())
        {
            await _cache.RemoveAsync(string.Format(PlatformConsts.UserPermissionCacheKey, userId));
        }
    }

    private static RoleDto Map(Role role) => new()
    {
        Id = role.Id,
        Code = role.Code,
        Name = role.Name,
        Remark = role.Remark,
        Status = role.Status,
        CreatedTime = role.CreatedTime
    };
}
