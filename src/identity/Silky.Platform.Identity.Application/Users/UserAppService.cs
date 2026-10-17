using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Silky.Core.Exceptions;
using Silky.Platform.Core.Consts;
using Silky.Platform.Core.Dtos;
using Silky.Platform.Identity.Application.Contracts.Users;
using Silky.Platform.Identity.Application.EntityFrameworkCore;
using Silky.Platform.Identity.Application.Entities;
using Silky.Platform.Identity.Application.Permissions;
using Silky.Platform.Identity.Application.Security;

namespace Silky.Platform.Identity.Application.Users;

public class UserAppService : IUserAppService
{
    private readonly IdentityDbContext _db;
    private readonly IDistributedCache _cache;
    private readonly UserPermissionResolver _permissionResolver;

    public UserAppService(
        IdentityDbContext db,
        IDistributedCache cache,
        UserPermissionResolver permissionResolver)
    {
        _db = db;
        _cache = cache;
        _permissionResolver = permissionResolver;
    }

    public async Task<PagedResult<UserDto>> GetPageAsync(GetUserPageInput input)
    {
        var query = _db.Users.AsNoTracking().Where(u => !u.IsDeleted);

        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            query = query.Where(u => u.UserName.Contains(input.Keyword)
                                     || (u.RealName != null && u.RealName.Contains(input.Keyword))
                                     || (u.Phone != null && u.Phone.Contains(input.Keyword)));
        }

        if (input.Status.HasValue)
        {
            query = query.Where(u => u.Status == input.Status.Value);
        }

        var total = await query.LongCountAsync();

        var users = await query
            .OrderByDescending(u => u.Id)
            .Skip((input.PageIndex - 1) * input.PageSize)
            .Take(input.PageSize)
            .ToListAsync();

        return new PagedResult<UserDto>(total, input.PageIndex, input.PageSize,
            await MapToDtosAsync(users));
    }

    public async Task<UserDto> GetAsync(long id)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
        if (user == null)
        {
            throw new BusinessException("用户不存在");
        }

        return (await MapToDtosAsync(new List<User> { user })).First();
    }

    public async Task<UserDto> CreateAsync(CreateUserInput input)
    {
        if (string.IsNullOrWhiteSpace(input.UserName))
        {
            throw new BusinessException("用户名不能为空");
        }

        if (string.IsNullOrWhiteSpace(input.Password))
        {
            throw new BusinessException("密码不能为空");
        }

        var exists = await _db.Users.AnyAsync(u => u.UserName == input.UserName && !u.IsDeleted);
        if (exists)
        {
            throw new BusinessException($"用户名 {input.UserName} 已存在");
        }

        var (hash, salt) = PasswordHasher.Hash(input.Password);

        var user = new User
        {
            UserName = input.UserName,
            PasswordHash = hash,
            PasswordSalt = salt,
            RealName = input.RealName,
            Email = input.Email,
            Phone = input.Phone,
            Remark = input.Remark,
            Status = 1,
            CreatedTime = DateTime.Now,
            CreatorId = CurrentUserAccessor.GetUserId()
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        await SetUserRolesInternalAsync(user.Id, input.RoleIds);

        return await GetAsync(user.Id);
    }

    public async Task<UserDto> UpdateAsync(UpdateUserInput input)
    {
        var user = await _db.Users.Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == input.Id && !u.IsDeleted);

        if (user == null)
        {
            throw new BusinessException("用户不存在");
        }

        user.RealName = input.RealName;
        user.Email = input.Email;
        user.Phone = input.Phone;
        user.Remark = input.Remark;
        user.UpdatedTime = DateTime.Now;
        user.UpdaterId = CurrentUserAccessor.GetUserId();

        await _db.SaveChangesAsync();

        await SetUserRolesInternalAsync(user.Id, input.RoleIds);

        return await GetAsync(user.Id);
    }

    public async Task DeleteAsync(long id)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
        if (user == null)
        {
            throw new BusinessException("用户不存在");
        }

        if (user.UserName == "admin")
        {
            throw new BusinessException("内置管理员账号不允许删除");
        }

        user.IsDeleted = true;
        user.UpdatedTime = DateTime.Now;
        await _db.SaveChangesAsync();

        await RemovePermissionCacheAsync(id);
    }

    public async Task ResetPasswordAsync(ResetPasswordInput input)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == input.Id && !u.IsDeleted);
        if (user == null)
        {
            throw new BusinessException("用户不存在");
        }

        var (hash, salt) = PasswordHasher.Hash(input.NewPassword);
        user.PasswordHash = hash;
        user.PasswordSalt = salt;
        user.UpdatedTime = DateTime.Now;
        user.UpdaterId = CurrentUserAccessor.GetUserId();
        await _db.SaveChangesAsync();

        await RemovePermissionCacheAsync(user.Id);
    }

    public async Task SetUserRolesAsync(SetUserRolesInput input)
    {
        await SetUserRolesInternalAsync(input.UserId, input.RoleIds);
    }

    public async Task SetUserStatusAsync(SetUserStatusInput input)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == input.UserId && !u.IsDeleted);
        if (user == null)
        {
            throw new BusinessException("用户不存在");
        }

        user.Status = input.Status;
        user.UpdatedTime = DateTime.Now;
        await _db.SaveChangesAsync();

        await RemovePermissionCacheAsync(user.Id);
    }

    private async Task SetUserRolesInternalAsync(long userId, IReadOnlyList<long> roleIds)
    {
        var exists = await _db.UserRoles.Where(ur => ur.UserId == userId).ToListAsync();
        _db.UserRoles.RemoveRange(exists);

        if (roleIds != null && roleIds.Count > 0)
        {
            foreach (var roleId in roleIds.Distinct())
            {
                _db.UserRoles.Add(new UserRole { UserId = userId, RoleId = roleId });
            }
        }

        await _db.SaveChangesAsync();
        await RemovePermissionCacheAsync(userId);
    }

    private async Task RemovePermissionCacheAsync(long userId)
    {
        await _cache.RemoveAsync(string.Format(PlatformConsts.UserPermissionCacheKey, userId));
    }

    private async Task<IReadOnlyList<UserDto>> MapToDtosAsync(IReadOnlyList<User> users)
    {
        var dtos = users.Select(u => new UserDto
        {
            Id = u.Id,
            UserName = u.UserName,
            RealName = u.RealName,
            Email = u.Email,
            Phone = u.Phone,
            Status = u.Status,
            Remark = u.Remark,
            CreatedTime = u.CreatedTime
        }).ToList();

        if (dtos.Count == 0)
        {
            return dtos;
        }

        var ids = dtos.Select(d => d.Id).ToList();
        var roleMap = await (from ur in _db.UserRoles.AsNoTracking()
                join r in _db.Roles.AsNoTracking() on ur.RoleId equals r.Id
                where ids.Contains(ur.UserId) && !r.IsDeleted
                select new { ur.UserId, r.Code })
            .ToListAsync();

        foreach (var dto in dtos)
        {
            dto.RoleCodes = roleMap.Where(x => x.UserId == dto.Id).Select(x => x.Code).ToList();
        }

        return dtos;
    }
}
