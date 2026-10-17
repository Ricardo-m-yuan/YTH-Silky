using Microsoft.EntityFrameworkCore;
using Silky.Core.Exceptions;
using Silky.Platform.Api.Application.Contracts.ApiGroups;
using Silky.Platform.Api.Application.EntityFrameworkCore;
using Silky.Platform.Api.Application.Entities;

namespace Silky.Platform.Api.Application.ApiGroups;

public class ApiGroupAppService : IApiGroupAppService
{
    private readonly ApiDbContext _db;

    public ApiGroupAppService(ApiDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ApiGroupDto>> GetAllAsync()
    {
        var groups = await _db.ApiGroups.AsNoTracking()
            .Where(g => !g.IsDeleted)
            .OrderBy(g => g.Sort)
            .ThenBy(g => g.Id)
            .ToListAsync();

        return groups.Select(Map).ToList();
    }

    public async Task<ApiGroupDto> GetAsync(long id)
    {
        var group = await _db.ApiGroups.AsNoTracking().FirstOrDefaultAsync(g => g.Id == id && !g.IsDeleted);
        if (group == null)
        {
            throw new BusinessException("分组不存在");
        }

        return Map(group);
    }

    public async Task<ApiGroupDto> CreateAsync(CreateApiGroupInput input)
    {
        if (await _db.ApiGroups.AnyAsync(g => g.Code == input.Code && !g.IsDeleted))
        {
            throw new BusinessException($"分组编码 {input.Code} 已存在");
        }

        var group = new ApiGroup
        {
            Code = input.Code,
            Name = input.Name,
            Sort = input.Sort,
            Remark = input.Remark,
            CreatedTime = DateTime.Now
        };

        _db.ApiGroups.Add(group);
        await _db.SaveChangesAsync();

        return Map(group);
    }

    public async Task<ApiGroupDto> UpdateAsync(UpdateApiGroupInput input)
    {
        var group = await _db.ApiGroups.FirstOrDefaultAsync(g => g.Id == input.Id && !g.IsDeleted);
        if (group == null)
        {
            throw new BusinessException("分组不存在");
        }

        group.Name = input.Name;
        group.Sort = input.Sort;
        group.Remark = input.Remark;
        group.UpdatedTime = DateTime.Now;
        await _db.SaveChangesAsync();

        return Map(group);
    }

    public async Task DeleteAsync(long id)
    {
        var group = await _db.ApiGroups.FirstOrDefaultAsync(g => g.Id == id && !g.IsDeleted);
        if (group == null)
        {
            throw new BusinessException("分组不存在");
        }

        if (await _db.ApiInterfaces.AnyAsync(x => x.GroupId == id && !x.IsDeleted))
        {
            throw new BusinessException("该分组下仍有关联接口，不能删除");
        }

        group.IsDeleted = true;
        await _db.SaveChangesAsync();
    }

    private static ApiGroupDto Map(ApiGroup g) => new()
    {
        Id = g.Id,
        Code = g.Code,
        Name = g.Name,
        Sort = g.Sort,
        Remark = g.Remark,
        CreatedTime = g.CreatedTime
    };
}
