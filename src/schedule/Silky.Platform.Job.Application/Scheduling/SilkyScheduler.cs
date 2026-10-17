using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Impl;
using Quartz.Impl.Matchers;
using Quartz.Spi;
using Silky.Platform.Job.Application.Entities;
using Silky.Platform.Job.Application.EntityFrameworkCore;

namespace Silky.Platform.Job.Application.Scheduling;

/// <summary>
/// Quartz 调度器封装：负责任务的调度生命周期与执行日志记录。
/// 设计要点：
///  - 单例 Scheduler，应用启动时从数据库加载所有 Running 任务并调度
///  - JobDetail 采用 DurableJob + 调度时绑定 Trigger，便于启停只切换 Trigger
///  - JobDataMap 传入 JobInfoId，执行器据此写日志、回写 LastRunTime/NextRunTime
/// </summary>
public class SilkyScheduler : ISilkyScheduler
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SilkyScheduler> _logger;
    private IScheduler _scheduler;

    public const string JobDataKeyJobId = "__jobId";

    public SilkyScheduler(IServiceProvider serviceProvider, ILogger<SilkyScheduler> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public IScheduler Scheduler => _scheduler;

    public async Task StartAsync()
    {
        var factory = new StdSchedulerFactory();
        _scheduler = await factory.GetScheduler();
        _scheduler.JobFactory = new SilkyJobFactory(_serviceProvider);
        await _scheduler.Start();

        await LoadAllJobsFromDatabaseAsync();
        _logger.LogInformation("SilkyScheduler started, loaded jobs from database.");
    }

    /// <summary>从数据库加载所有运行中任务到调度器</summary>
    private async Task LoadAllJobsFromDatabaseAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobDbContext>();
        var jobs = db.JobInfos.Where(x => !x.IsDeleted && x.Status == 1).ToList();
        foreach (var job in jobs)
        {
            try
            {
                await ScheduleJobAsync(job);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Load job {JobId} failed", job.Id);
            }
        }
    }

    /// <summary>调度任务（若已存在则先卸载再重建）</summary>
    public async Task ScheduleJobAsync(JobInfo job)
    {
        var jobKey = new JobKey(job.Name, job.Group);

        // 卸载旧的
        if (await _scheduler.CheckExists(jobKey))
        {
            await _scheduler.DeleteJob(jobKey);
        }

        var jobType = ResolveJobType(job.JobType);
        var jobDetail = JobBuilder.Create(jobType)
            .WithIdentity(jobKey)
            .UsingJobData(JobDataKeyJobId, job.Id)
            .StoreDurably()
            .Build();

        var trigger = TriggerBuilder.Create()
            .WithIdentity($"trigger-{job.Name}", job.Group)
            .WithCronSchedule(job.Cron)
            .Build();

        await _scheduler.AddJob(jobDetail, true);

        if (job.Status == 1)
        {
            await _scheduler.ScheduleJob(jobDetail, trigger);
        }
    }

    public async Task UnscheduleJobAsync(JobKey jobKey)
    {
        if (await _scheduler.CheckExists(jobKey))
        {
            await _scheduler.DeleteJob(jobKey);
        }
    }

    public async Task PauseAsync(JobKey jobKey)
    {
        await _scheduler.PauseJob(jobKey);
    }

    public async Task ResumeAsync(JobKey jobKey)
    {
        await _scheduler.ResumeJob(jobKey);
    }

    public async Task TriggerAsync(JobKey jobKey)
    {
        await _scheduler.TriggerJob(jobKey);
    }

    public async Task<DateTime?> GetNextRunTimeAsync(JobKey jobKey)
    {
        var triggers = await _scheduler.GetTriggersOfJob(jobKey);
        if (triggers == null || !triggers.Any()) return null;
        var next = triggers.Select(t => t.GetNextFireTimeUtc()?.LocalDateTime)
            .Where(x => x.HasValue).OrderBy(x => x).FirstOrDefault();
        return next;
    }

    private static Type ResolveJobType(string fullName)
    {
        var t = Type.GetType(fullName);
        if (t != null) return t;
        var asm = AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetType(fullName)).FirstOrDefault(x => x != null);
        if (asm != null) return asm;
        throw new InvalidOperationException($"Job type not found: {fullName}. Ensure the assembly is referenced.");
    }
}

public interface ISilkyScheduler
{
    IScheduler Scheduler { get; }
    Task StartAsync();
    Task ScheduleJobAsync(JobInfo job);
    Task UnscheduleJobAsync(JobKey jobKey);
    Task PauseAsync(JobKey jobKey);
    Task ResumeAsync(JobKey jobKey);
    Task TriggerAsync(JobKey jobKey);
    Task<DateTime?> GetNextRunTimeAsync(JobKey jobKey);
}

/// <summary>
/// DI 感知的 JobFactory：每次触发时创建 scope，执行完释放。
/// </summary>
public class SilkyJobFactory : IJobFactory
{
    private readonly IServiceProvider _serviceProvider;
    public SilkyJobFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
    {
        return new ScopedJobWrapper(_serviceProvider.CreateScope(), bundle.JobDetail.JobType);
    }

    public void ReturnJob(IJob job)
    {
        (job as IDisposable)?.Dispose();
    }
}

/// <summary>在 scope 内创建 Job 实例并执行，结束后 dispose scope</summary>
public class ScopedJobWrapper : IJob, IDisposable
{
    private readonly IServiceScope _scope;
    private readonly Type _jobType;

    public ScopedJobWrapper(IServiceScope scope, Type jobType)
    {
        _scope = scope;
        _jobType = jobType;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var job = (IJob)_scope.ServiceProvider.GetRequiredService(_jobType);
        try
        {
            await job.Execute(context);
        }
        finally
        {
            // Quartz 不主动释放；scope 在 Dispose 中释放
        }
    }

    public void Dispose()
    {
        _scope?.Dispose();
    }
}

/// <summary>
/// Job 监听器：统一记录执行日志、回写 LastRunTime/NextRunTime。
/// </summary>
public class JobExecutionListener : IJobListener
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<JobExecutionListener> _logger;

    public JobExecutionListener(IServiceProvider serviceProvider, ILogger<JobExecutionListener> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public string Name => "SilkyJobListener";

    public Task JobToBeExecuted(IJobExecutionContext context, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public async Task JobWasExecuted(IJobExecutionContext context, JobExecutionException jobException, CancellationToken cancellationToken = default)
    {
        var jobId = context.JobDetail.JobDataMap.GetLongValue(SilkyScheduler.JobDataKeyJobId);
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobDbContext>();

        var status = jobException != null ? 0 : 1;
        var message = jobException != null ? jobException.ToString() : "OK";
        var elapsed = (long)context.JobRunTime.TotalMilliseconds;

        db.JobLogs.Add(new JobLog
        {
            JobId = jobId,
            JobName = context.JobDetail.Key.Name,
            Status = status,
            Message = message,
            ElapsedMs = elapsed,
            StartTime = context.FireTimeUtc.LocalDateTime,
            EndTime = DateTime.Now
        });

        var jobInfo = db.JobInfos.Find(jobId);
        if (jobInfo != null)
        {
            jobInfo.LastRunTime = context.FireTimeUtc.LocalDateTime;
            jobInfo.NextRunTime = context.NextFireTimeUtc?.LocalDateTime;
        }

        await db.SaveChangesAsync(cancellationToken);

        if (jobException != null)
            _logger.LogError(jobException, "Job {JobName} failed", context.JobDetail.Key.Name);
        else
            _logger.LogInformation("Job {JobName} done in {Elapsed}ms", context.JobDetail.Key.Name, elapsed);
    }

    public Task JobExecutionVetoed(IJobExecutionContext context, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
