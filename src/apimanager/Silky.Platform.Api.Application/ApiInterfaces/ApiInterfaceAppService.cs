using Microsoft.EntityFrameworkCore;
using Silky.Core.Exceptions;
using Silky.Platform.Api.Application.Contracts.ApiInterfaces;
using Silky.Platform.Api.Application.EntityFrameworkCore;
using Silky.Platform.Api.Application.Entities;
using Silky.Platform.Core.Dtos;
using Silky.Rpc.Runtime.Server;

namespace Silky.Platform.Api.Application.ApiInterfaces;

/// <summary>
/// 接口管理：从 Silky 服务条目采集接口清单，并把管控配置写回服务条目使其运行时生效
/// </summary>
public class ApiInterfaceAppService : IApiInterfaceAppService
{
    private readonly ApiDbContext _db;
    private readonly IServiceEntryManager _serviceEntryManager;

    public ApiInterfaceAppService(ApiDbContext db, IServiceEntryManager serviceEntryManager)
    {
        _db = db;
        _serviceEntryManager = serviceEntryManager;
    }

    public async Task<PagedResult<ApiInterfaceDto>> GetPageAsync(GetApiInterfacePageInput input)
    {
        var query = _db.ApiInterfaces.AsNoTracking().Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(input.ServiceName))
        {
            query = query.Where(x => x.ServiceName == input.ServiceName);
        }

        if (input.GroupId.HasValue)
        {
            query = query.Where(x => x.GroupId == input.GroupId.Value);
        }

        if (input.IsEnabled.HasValue)
        {
            query = query.Where(x => x.IsEnabled == input.IsEnabled.Value);
        }

        if (!string.IsNullOrWhiteSpace(input.HttpMethod))
        {
            query = query.Where(x => x.HttpMethod == input.HttpMethod.ToUpperInvariant());
        }

        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            query = query.Where(x => x.WebApi.Contains(input.Keyword)
                                     || (x.Method != null && x.Method.Contains(input.Keyword))
                                     || (x.ServiceName != null && x.ServiceName.Contains(input.Keyword)));
        }

        var total = await query.LongCountAsync();
        var list = await query
            .OrderBy(x => x.ServiceName)
            .ThenBy(x => x.WebApi)
            .Skip((input.PageIndex - 1) * input.PageSize)
            .Take(input.PageSize)
            .ToListAsync();

        return new PagedResult<ApiInterfaceDto>(total, input.PageIndex, input.PageSize, await MapAsync(list));
    }

    public async Task<ApiInterfaceDto> GetAsync(long id)
    {
        var entity = await _db.ApiInterfaces.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (entity == null)
        {
            throw new BusinessException("接口不存在");
        }

        return (await MapAsync(new List<ApiInterface> { entity })).First();
    }

    public async Task<IReadOnlyList<ApiInterfaceDto>> GetListByServiceAsync(string serviceName)
    {
        var list = await _db.ApiInterfaces.AsNoTracking()
            .Where(x => !x.IsDeleted && x.ServiceName == serviceName)
            .OrderBy(x => x.WebApi)
            .ToListAsync();

        return await MapAsync(list);
    }

    public async Task<ApiInterfaceDto> UpdateAsync(UpdateApiInterfaceInput input)
    {
        var entity = await _db.ApiInterfaces.FirstOrDefaultAsync(x => x.Id == input.Id && !x.IsDeleted);
        if (entity == null)
        {
            throw new BusinessException("接口不存在");
        }

        entity.GroupId = input.GroupId;
        entity.IsEnabled = input.IsEnabled;
        entity.RequireAuth = input.RequireAuth;
        entity.PermissionCode = input.PermissionCode;
        entity.TimeoutMs = input.TimeoutMs;
        entity.MaxConcurrent = input.MaxConcurrent;
        entity.EnableCache = input.EnableCache;
        entity.Remark = input.Remark;
        entity.UpdatedTime = DateTime.Now;

        await _db.SaveChangesAsync();

        // 写回服务条目，使配置运行时生效（仅本地条目可即时生效）
        ApplyToServiceEntry(entity);

        return await GetAsync(entity.Id);
    }

    public async Task SetEnabledAsync(SetApiEnabledInput input)
    {
        var entity = await _db.ApiInterfaces.FirstOrDefaultAsync(x => x.Id == input.Id && !x.IsDeleted);
        if (entity == null)
        {
            throw new BusinessException("接口不存在");
        }

        entity.IsEnabled = input.IsEnabled;
        entity.UpdatedTime = DateTime.Now;
        await _db.SaveChangesAsync();

        ApplyToServiceEntry(entity);
    }

    public async Task<SyncApiResultDto> SyncAsync(SyncApiInput input)
    {
        var entries = input.OnlyLocal
            ? _serviceEntryManager.GetLocalEntries()
            : _serviceEntryManager.GetAllEntries();

        var result = new SyncApiResultDto { Total = entries?.Count ?? 0 };

        if (entries == null || entries.Count == 0)
        {
            return result;
        }

        foreach (var entry in entries)
        {
            var descriptor = entry.ServiceEntryDescriptor;
            if (descriptor == null || string.IsNullOrEmpty(descriptor.Id))
            {
                continue;
            }

            var httpMethod = descriptor.HttpMethod?.ToString()?.ToUpperInvariant();
            var governance = entry.GovernanceOptions;

            var entity = await _db.ApiInterfaces
                .FirstOrDefaultAsync(x => x.ServiceEntryId == descriptor.Id);

            if (entity == null)
            {
                _db.ApiInterfaces.Add(new ApiInterface
                {
                    ServiceEntryId = descriptor.Id,
                    ServiceId = descriptor.ServiceId,
                    ServiceName = descriptor.ServiceName,
                    Method = descriptor.Method,
                    WebApi = descriptor.WebApi,
                    HttpMethod = httpMethod,
                    IsEnabled = !descriptor.ProhibitExtranet,
                    RequireAuth = !descriptor.IsAllowAnonymous,
                    TimeoutMs = governance?.TimeoutMillSeconds ?? 5000,
                    MaxConcurrent = governance?.MaxConcurrentHandlingCount ?? 100,
                    EnableCache = governance?.EnableCachingInterceptor ?? false,
                    IsLocal = entry.IsLocal,
                    CreatedTime = DateTime.Now
                });
                result.Added++;
                continue;
            }

            // 已存在时只刷新框架侧元数据，不覆盖人工维护的分组/权限/启停等配置
            var changed = false;

            if (entity.Method != descriptor.Method)
            {
                entity.Method = descriptor.Method;
                changed = true;
            }

            if (entity.WebApi != descriptor.WebApi)
            {
                entity.WebApi = descriptor.WebApi;
                changed = true;
            }

            if (entity.HttpMethod != httpMethod)
            {
                entity.HttpMethod = httpMethod;
                changed = true;
            }

            if (entity.IsLocal != entry.IsLocal)
            {
                entity.IsLocal = entry.IsLocal;
                changed = true;
            }

            if (changed)
            {
                entity.UpdatedTime = DateTime.Now;
                result.Updated++;
            }
            else
            {
                result.Unchanged++;
            }
        }

        await _db.SaveChangesAsync();
        return result;
    }

    /// <summary>
    /// 把管控配置写回 Silky 服务条目，实现运行时生效。
    /// 注意：IServiceEntryManager.Update 对本进程内的本地条目即时生效；远程服务条目需在各自进程内同步。
    /// </summary>
    private void ApplyToServiceEntry(ApiInterface entity)
    {
        if (string.IsNullOrEmpty(entity.ServiceEntryId))
        {
            return;
        }

        var entry = _serviceEntryManager.GetServiceEntry(entity.ServiceEntryId);
        if (entry?.GovernanceOptions == null)
        {
            return;
        }

        entry.GovernanceOptions.ProhibitExtranet = !entity.IsEnabled;
        entry.GovernanceOptions.TimeoutMillSeconds = entity.TimeoutMs;
        entry.GovernanceOptions.MaxConcurrentHandlingCount = entity.MaxConcurrent;
        entry.GovernanceOptions.EnableCachingInterceptor = entity.EnableCache;

        _serviceEntryManager.Update(entry);
    }

    private async Task<IReadOnlyList<ApiInterfaceDto>> MapAsync(IReadOnlyList<ApiInterface> list)
    {
        if (list.Count == 0)
        {
            return new List<ApiInterfaceDto>();
        }

        var groupIds = list.Where(x => x.GroupId.HasValue).Select(x => x.GroupId.Value).Distinct().ToList();
        var groups = groupIds.Count == 0
            ? new Dictionary<long, string>()
            : await _db.ApiGroups.AsNoTracking()
                .Where(g => groupIds.Contains(g.Id))
                .ToDictionaryAsync(g => g.Id, g => g.Name);

        return list.Select(x => new ApiInterfaceDto
        {
            Id = x.Id,
            ServiceEntryId = x.ServiceEntryId,
            ServiceId = x.ServiceId,
            ServiceName = x.ServiceName,
            Method = x.Method,
            WebApi = x.WebApi,
            HttpMethod = x.HttpMethod,
            GroupId = x.GroupId,
            GroupName = x.GroupId.HasValue && groups.ContainsKey(x.GroupId.Value) ? groups[x.GroupId.Value] : null,
            IsEnabled = x.IsEnabled,
            RequireAuth = x.RequireAuth,
            PermissionCode = x.PermissionCode,
            TimeoutMs = x.TimeoutMs,
            MaxConcurrent = x.MaxConcurrent,
            EnableCache = x.EnableCache,
            IsLocal = x.IsLocal,
            Remark = x.Remark,
            CreatedTime = x.CreatedTime,
            UpdatedTime = x.UpdatedTime
        }).ToList();
    }
}
