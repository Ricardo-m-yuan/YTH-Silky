using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quartz;
using Quartz.Impl.Matchers;
using Silky.Platform.Job.Application.EntityFrameworkCore;
using Silky.Platform.Job.Application.Scheduling;

namespace Silky.Platform.Job.Host;

public class JobHostConfigureService : IConfigureService
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<JobDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("default"),
                sql => sql.MigrationsAssembly(typeof(JobHostConfigureService).Assembly.GetName().Name)));

        // Quartz 调度
        services.AddSingleton<ISilkyScheduler, SilkyScheduler>();
        services.AddSingleton<JobExecutionListener>();
        services.AddHostedService<SchedulerHostedService>();

        // 注册内置 Job 类型到 DI（执行器通过 DI 拿到 Logger 等依赖）
        services.AddTransient<Silky.Platform.Job.Application.Jobs.SampleJob>();
        services.AddTransient<Silky.Platform.Job.Application.Jobs.HttpCallJob>();
    }
}

/// <summary>
/// 调度器启动器：在主机启动时 Start，停止时 Shutdown。
/// </summary>
public class SchedulerHostedService : IHostedService
{
    private readonly ISilkyScheduler _scheduler;
    private readonly JobExecutionListener _listener;
    public SchedulerHostedService(ISilkyScheduler scheduler, JobExecutionListener listener)
    {
        _scheduler = scheduler;
        _listener = listener;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _scheduler.StartAsync();
        _scheduler.Scheduler.ListenerManager.AddJobListener(_listener, GroupMatcher<JobKey>.AnyGroup());
    }

    public async Task ShutdownAsync(CancellationToken cancellationToken)
    {
        await _scheduler.Scheduler.Shutdown(true);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
