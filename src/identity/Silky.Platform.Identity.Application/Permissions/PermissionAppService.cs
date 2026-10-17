using Microsoft.EntityFrameworkCore;
using Silky.Core.Exceptions;
using Silky.Platform.Core.Dtos;
using Silky.Platform.Identity.Application.Contracts.Permissions;
using Silky.Platform.Identity.Application.EntityFrameworkCore;
using Silky.Platform.Identity.Application.Entities;
using Silky.Platform.Identity.Application.Security;

namespace Silky.Platform.Identity.Application.Permissions;

public class PermissionAppService : IPermissionAppService
{
    private readonly IdentityDbContext _db;
    private readonly UserPermissionResolver _permissionResolver;

    public PermissionAppService(IdentityDbContext db, UserPermissionResolver permissionResolver)
    {
        _db = db;
        _permissionResolver = permissionResolver;
    }

    public async Task<PagedResult<PermissionDto>> GetPageAsync(GetPermissionPageInput input)
    {
        var query = _db.Permissions.AsNoTracking().Where(p => !p.IsDeleted);

        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            query = query.Where(p => p.Code.Contains(input.Keyword) || p.Name.Contains(input.Keyword));
        }

        if (input.Type.HasValue)
        {
            query = query.Where(p => p.Type == input.Type.Value);
        }

        if (!string.IsNullOrWhiteSpace(input.Group))
        {
            query = query.Where(p => p.Group == input.Group);
        }

        var total = await query.LongCountAsync();
        var permissions = await query
            .OrderBy(p => p.Sort)
            .ThenBy(p => p.Id)
            .Skip((input.PageIndex - 1) * input.PageSize)
            .Take(input.PageSize)
            .ToListAsync();

        return new PagedResult<PermissionDto>(total, input.PageIndex, input.PageSize,
            permissions.Select(Map).ToList());
    }

    public async Task<IReadOnlyList<PermissionDto>> GetAllAsync()
    {
        var permissions = await _db.Permissions.AsNoTracking()
            .Where(p => !p.IsDeleted)
            .OrderBy(p => p.Sort)
            .ThenBy(p => p.Id)
            .ToListAsync();

        return permissions.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<PermissionTreeNodeDto>> GetTreeAsync()
    {
        var permissions = await _db.Permissions.AsNoTracking()
            .Where(p => !p.IsDeleted)
            .OrderBy(p => p.Sort)
            .ThenBy(p => p.Id)
            .ToListAsync();

        var nodes = permissions.Select(p => new PermissionTreeNodeDto
        {
            Id = p.Id,
            Code = p.Code,
            Name = p.Name,
            Type = p.Type,
            Group = p.Group,
            ParentId = p.ParentId,
            Sort = p.Sort
        }).ToList();

        var lookup = nodes.ToLookup(n => n.ParentId);

        foreach (var node in nodes)
        {
            node.Children = lookup[node.Id].ToList();
        }

        // 顶层：ParentId 为空，或父节点已不存在
        var ids = nodes.Select(n => n.Id).ToHashSet();
        return nodes.Where(n => n.ParentId == null || !ids.Contains(n.ParentId.Value)).ToList();
    }

    public async Task<PermissionDto> CreateAsync(CreatePermissionInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Code))
        {
            throw new BusinessException("权限码不能为空");
        }

        if (await _db.Permissions.AnyAsync(p => p.Code == input.Code && !p.IsDeleted))
        {
            throw new BusinessException($"权限码 {input.Code} 已存在");
        }

        var permission = new Permission
        {
            Code = input.Code,
            Name = input.Name,
            Type = input.Type,
            Group = input.Group,
            ParentId = input.ParentId,
            Sort = input.Sort,
            Remark = input.Remark,
            CreatedTime = DateTime.Now,
            CreatorId = CurrentUserAccessor.GetUserId()
        };

        _db.Permissions.Add(permission);
        await _db.SaveChangesAsync();

        return Map(permission);
    }

    public async Task<PermissionDto> UpdateAsync(UpdatePermissionInput input)
    {
        var permission = await _db.Permissions.FirstOrDefaultAsync(p => p.Id == input.Id && !p.IsDeleted);
        if (permission == null)
        {
            throw new BusinessException("权限不存在");
        }

        permission.Name = input.Name;
        permission.Type = input.Type;
        permission.Group = input.Group;
        permission.ParentId = input.ParentId;
        permission.Sort = input.Sort;
        permission.Remark = input.Remark;
        permission.UpdatedTime = DateTime.Now;
        permission.UpdaterId = CurrentUserAccessor.GetUserId();

        await _db.SaveChangesAsync();
        return Map(permission);
    }

    public async Task DeleteAsync(long id)
    {
        var permission = await _db.Permissions.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
        if (permission == null)
        {
            throw new BusinessException("权限不存在");
        }

        if (await _db.Permissions.AnyAsync(p => p.ParentId == id && !p.IsDeleted))
        {
            throw new BusinessException("存在子权限，不能删除");
        }

        permission.IsDeleted = true;
        await _db.SaveChangesAsync();
    }

    /// <summary>供网关等内部服务通过 RPC 调用，不对外暴露 HTTP</summary>
    public async Task<IReadOnlyList<string>> GetUserPermissionCodesAsync(long userId)
    {
        return await _permissionResolver.GetPermissionCodesAsync(userId);
    }

    /// <summary>供网关等内部服务通过 RPC 调用，不对外暴露 HTTP</summary>
    public async Task<bool> CheckUserPermissionAsync(long userId, string permissionCode)
    {
        if (string.IsNullOrWhiteSpace(permissionCode))
        {
            return true;
        }

        var permissions = await _permissionResolver.GetPermissionCodesAsync(userId);
        return permissions.Contains(permissionCode);
    }

    private static PermissionDto Map(Permission p) => new()
    {
        Id = p.Id,
        Code = p.Code,
        Name = p.Name,
        Type = p.Type,
        Group = p.Group,
        ParentId = p.ParentId,
        Sort = p.Sort,
        Remark = p.Remark,
        CreatedTime = p.CreatedTime
    };
}
