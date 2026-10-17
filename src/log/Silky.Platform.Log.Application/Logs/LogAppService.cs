using Microsoft.EntityFrameworkCore;
using Silky.Platform.Contracts.Consts;
using Silky.Platform.Core.Dtos;
using Silky.Platform.Log.Application.Contracts.Logs;
using Silky.Platform.Log.Application.EntityFrameworkCore;
using Silky.Platform.Log.Application.Entities;
using Silky.Rpc.Routing;

namespace Silky.Platform.Log.Application.Logs;

public class LogAppService : ILogAppService
{
    private readonly LogDbContext _db;

    public LogAppService(LogDbContext db)
    {
        _db = db;
    }

    public Task<PagedResult<OperationLogDto>> GetOperationLogPageAsync(GetOperationLogInput input)
    {
        var q = _db.OperationLogs.AsNoTracking();
        if (input.UserId.HasValue) q = q.Where(x => x.UserId == input.UserId.Value);
        if (!string.IsNullOrWhiteSpace(input.ServiceName))
            q = q.Where(x => x.ServiceName.Contains(input.ServiceName));
        if (!string.IsNullOrWhiteSpace(input.Keyword))
            q = q.Where(x => x.ApiPath.Contains(input.Keyword) || x.UserName.Contains(input.Keyword));
        if (input.StartTime.HasValue) q = q.Where(x => x.OperateTime >= input.StartTime.Value);
        if (input.EndTime.HasValue) q = q.Where(x => x.OperateTime <= input.EndTime.Value);

        var total = q.LongCount();
        var items = q.OrderByDescending(x => x.OperateTime)
            .Skip((input.PageIndex - 1) * input.PageSize)
            .Take(input.PageSize)
            .Select(MapOperationLog)
            .ToList();

        return Task.FromResult(new PagedResult<OperationLogDto>(total, input.PageIndex, input.PageSize, items));
    }

    public Task<PagedResult<LoginLogDto>> GetLoginLogPageAsync(GetLoginLogInput input)
    {
        var q = _db.LoginLogs.AsNoTracking();
        if (input.UserId.HasValue) q = q.Where(x => x.UserId == input.UserId.Value);
        if (input.Status.HasValue) q = q.Where(x => x.Status == input.Status.Value);
        if (!string.IsNullOrWhiteSpace(input.Keyword))
            q = q.Where(x => x.UserName.Contains(input.Keyword) || x.Ip.Contains(input.Keyword));
        if (input.StartTime.HasValue) q = q.Where(x => x.LoginTime >= input.StartTime.Value);
        if (input.EndTime.HasValue) q = q.Where(x => x.LoginTime <= input.EndTime.Value);

        var total = q.LongCount();
        var items = q.OrderByDescending(x => x.LoginTime)
            .Skip((input.PageIndex - 1) * input.PageSize)
            .Take(input.PageSize)
            .Select(MapLoginLog)
            .ToList();

        return Task.FromResult(new PagedResult<LoginLogDto>(total, input.PageIndex, input.PageSize, items));
    }

    public Task<PagedResult<ExceptionLogDto>> GetExceptionLogPageAsync(GetExceptionLogInput input)
    {
        var q = _db.ExceptionLogs.AsNoTracking();
        if (input.UserId.HasValue) q = q.Where(x => x.UserId == input.UserId.Value);
        if (!string.IsNullOrWhiteSpace(input.ServiceName))
            q = q.Where(x => x.ServiceName.Contains(input.ServiceName));
        if (!string.IsNullOrWhiteSpace(input.Keyword))
            q = q.Where(x => x.Message.Contains(input.Keyword) || x.UserName.Contains(input.Keyword));
        if (input.StartTime.HasValue) q = q.Where(x => x.OccurTime >= input.StartTime.Value);
        if (input.EndTime.HasValue) q = q.Where(x => x.OccurTime <= input.EndTime.Value);

        var total = q.LongCount();
        var items = q.OrderByDescending(x => x.OccurTime)
            .Skip((input.PageIndex - 1) * input.PageSize)
            .Take(input.PageSize)
            .Select(MapExceptionLog)
            .ToList();

        return Task.FromResult(new PagedResult<ExceptionLogDto>(total, input.PageIndex, input.PageSize, items));
    }

    public async Task ClearOperationLogAsync()
    {
        await _db.Database.ExecuteSqlRawAsync("TRUNCATE TABLE OperationLog");
    }

    public async Task ClearLoginLogAsync()
    {
        await _db.Database.ExecuteSqlRawAsync("TRUNCATE TABLE LoginLog");
    }

    public async Task ClearExceptionLogAsync()
    {
        await _db.Database.ExecuteSqlRawAsync("TRUNCATE TABLE ExceptionLog");
    }

    private static OperationLogDto MapOperationLog(OperationLog x) => new()
    {
        Id = x.Id, UserId = x.UserId, UserName = x.UserName, ServiceName = x.ServiceName,
        ApiPath = x.ApiPath, HttpMethod = x.HttpMethod, Ip = x.Ip, StatusCode = x.StatusCode,
        ElapsedMs = x.ElapsedMs, RequestBody = x.RequestBody, ResponseBody = x.ResponseBody,
        OperateTime = x.OperateTime
    };

    private static LoginLogDto MapLoginLog(LoginLog x) => new()
    {
        Id = x.Id, UserId = x.UserId, UserName = x.UserName, Ip = x.Ip, Browser = x.Browser,
        Os = x.Os, Status = x.Status, Message = x.Message, LoginTime = x.LoginTime
    };

    private static ExceptionLogDto MapExceptionLog(ExceptionLog x) => new()
    {
        Id = x.Id, UserId = x.UserId, UserName = x.UserName, ServiceName = x.ServiceName,
        ApiPath = x.ApiPath, HttpMethod = x.HttpMethod, Ip = x.Ip, ExceptionType = x.ExceptionType,
        Message = x.Message, StackTrace = x.StackTrace, OccurTime = x.OccurTime
    };
}
