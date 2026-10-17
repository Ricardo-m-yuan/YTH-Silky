using Microsoft.EntityFrameworkCore;
using Quartz;
using Silky.Platform.Core.Dtos;
using Silky.Platform.Job.Application.Contracts.Jobs;
using Silky.Platform.Job.Application.Entities;
using Silky.Platform.Job.Application.EntityFrameworkCore;
using Silky.Platform.Job.Application.Scheduling;
using Silky.Core.Exceptions;
using Silky.Rpc.Routing;

namespace Silky.Platform.Job.Application.Jobs;

public class JobAppService : IJobAppService
{
    private readonly JobDbContext _db;
    private readonly ISilkyScheduler _scheduler;

    public JobAppService(JobDbContext db, ISilkyScheduler scheduler)
    {
        _db = db;
        _scheduler = scheduler;
    }

    public Task<PagedResult<JobInfoDto>> GetPageAsync(GetJobPageInput input)
    {
        var q = _db.JobInfos.Where(x => !x.IsDeleted).AsNoTracking();
        if (!string.IsNullOrWhiteSpace(input.Keyword))
            q = q.Where(x => x.Name.Contains(input.Keyword) || x.JobType.Contains(input.Keyword));
        if (!string.IsNullOrWhiteSpace(input.Group))
            q = q.Where(x => x.Group == input.Group);
        if (input.Status.HasValue)
            q = q.Where(x => x.Status == input.Status.Value);

        var total = q.LongCount();
        var items = q.OrderBy(x => x.Id)
            .Skip((input.PageIndex - 1) * input.PageSize)
            .Take(input.PageSize)
            .Select(MapInfo)
            .ToList();

        return Task.FromResult(new PagedResult<JobInfoDto>(total, input.PageIndex, input.PageSize, items));
    }

    public Task<IReadOnlyList<JobInfoDto>> GetAllAsync()
    {
        var list = _db.JobInfos.Where(x => !x.IsDeleted).AsNoTracking()
            .OrderBy(x => x.Id)
            .Select(MapInfo)
            .ToList();
        return Task.FromResult<IReadOnlyList<JobInfoDto>>(list);
    }

    public Task<JobInfoDto> GetAsync(long id)
    {
        var entity = _db.JobInfos.Find(id);
        if (entity == null || entity.IsDeleted)
            throw new BusinessException($"Job {id} not found");
        return Task.FromResult(MapInfo(entity));
    }

    public async Task<JobInfoDto> CreateAsync(CreateJobInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Name) || string.IsNullOrWhiteSpace(input.JobType))
            throw new BusinessException("Name and JobType are required.");
        if (string.IsNullOrWhiteSpace(input.Cron))
            throw new BusinessException("Cron is required.");

        var entity = new JobInfo
        {
            Name = input.Name,
            Group = string.IsNullOrWhiteSpace(input.Group) ? "default" : input.Group,
            Cron = input.Cron,
            JobType = input.JobType,
            JobData = input.JobData,
            Status = input.Status,
            Description = input.Description,
            CreatedTime = DateTime.Now
        };

        _db.JobInfos.Add(entity);
        await _db.SaveChangesAsync();

        if (entity.Status == JobStatus.Running)
        {
            await _scheduler.ScheduleJobAsync(entity);
        }

        return MapInfo(entity);
    }

    public async Task<JobInfoDto> UpdateAsync(UpdateJobInput input)
    {
        var entity = _db.JobInfos.Find(input.Id);
        if (entity == null || entity.IsDeleted)
            throw new BusinessException($"Job {input.Id} not found");

        entity.Name = input.Name;
        entity.Group = string.IsNullOrWhiteSpace(input.Group) ? "default" : input.Group;
        entity.Cron = input.Cron;
        entity.JobType = input.JobType;
        entity.JobData = input.JobData;
        entity.Description = input.Description;
        entity.UpdatedTime = DateTime.Now;

        await _db.SaveChangesAsync();

        // 重新调度
        await _scheduler.ScheduleJobAsync(entity);

        return MapInfo(entity);
    }

    public async Task DeleteAsync(long id)
    {
        var entity = _db.JobInfos.Find(id);
        if (entity == null || entity.IsDeleted)
            return;

        entity.IsDeleted = true;
        entity.UpdatedTime = DateTime.Now;
        await _db.SaveChangesAsync();

        await _scheduler.UnscheduleJobAsync(new JobKey(entity.Name, entity.Group));
    }

    public async Task SetStatusAsync(SetJobStatusInput input)
    {
        var entity = _db.JobInfos.Find(input.Id);
        if (entity == null || entity.IsDeleted)
            throw new BusinessException($"Job {input.Id} not found");

        entity.Status = input.Status;
        entity.UpdatedTime = DateTime.Now;
        await _db.SaveChangesAsync();

        var jobKey = new JobKey(entity.Name, entity.Group);
        if (input.Status == JobStatus.Stopped)
        {
            await _scheduler.PauseAsync(jobKey);
        }
        else
        {
            await _scheduler.ResumeAsync(jobKey);
        }
    }

    public async Task TriggerAsync(TriggerJobInput input)
    {
        var entity = _db.JobInfos.Find(input.Id);
        if (entity == null || entity.IsDeleted)
            throw new BusinessException($"Job {input.Id} not found");

        await _scheduler.TriggerAsync(new JobKey(entity.Name, entity.Group));
    }

    public Task<PagedResult<JobLogDto>> GetLogPageAsync(GetJobLogPageInput input)
    {
        var q = _db.JobLogs.AsNoTracking();
        if (input.JobId.HasValue)
            q = q.Where(x => x.JobId == input.JobId.Value);
        if (input.Status.HasValue)
            q = q.Where(x => x.Status == input.Status.Value);

        var total = q.LongCount();
        var items = q.OrderByDescending(x => x.StartTime)
            .Skip((input.PageIndex - 1) * input.PageSize)
            .Take(input.PageSize)
            .Select(MapLog)
            .ToList();

        return Task.FromResult(new PagedResult<JobLogDto>(total, input.PageIndex, input.PageSize, items));
    }

    public async Task ClearLogAsync(ClearJobLogInput input)
    {
        var logs = _db.JobLogs.Where(x => x.JobId == input.JobId);
        _db.JobLogs.RemoveRange(logs);
        await _db.SaveChangesAsync();
    }

    private static JobInfoDto MapInfo(JobInfo x) => new()
    {
        Id = x.Id,
        Name = x.Name,
        Group = x.Group,
        Cron = x.Cron,
        JobType = x.JobType,
        JobData = x.JobData,
        Status = x.Status,
        Description = x.Description,
        LastRunTime = x.LastRunTime,
        NextRunTime = x.NextRunTime,
        CreatedTime = x.CreatedTime
    };

    private static JobLogDto MapLog(JobLog x) => new()
    {
        Id = x.Id,
        JobId = x.JobId,
        JobName = x.JobName,
        Status = x.Status,
        Message = x.Message,
        ElapsedMs = x.ElapsedMs,
        StartTime = x.StartTime,
        EndTime = x.EndTime
    };
}
