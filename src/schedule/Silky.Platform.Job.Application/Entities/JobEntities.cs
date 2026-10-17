using Silky.Platform.Core.Entities;

namespace Silky.Platform.Job.Application.Entities;

/// <summary>定时任务定义</summary>
public class JobInfo : EntityBase
{
    /// <summary>任务名（Quartz JobKey.Name）</summary>
    public string Name { get; set; }

    /// <summary>任务分组（Quartz JobKey.Group）</summary>
    public string Group { get; set; } = "default";

    /// <summary>CRON 表达式</summary>
    public string Cron { get; set; }

    /// <summary>执行器类型全名，需实现 Quartz.IJob</summary>
    public string JobType { get; set; }

    /// <summary>执行参数（JSON）</summary>
    public string JobData { get; set; }

    /// <summary>0=停止 1=运行</summary>
    public int Status { get; set; }

    public string Description { get; set; }

    public DateTime? LastRunTime { get; set; }

    public DateTime? NextRunTime { get; set; }
}

/// <summary>任务执行日志</summary>
public class JobLog
{
    public long Id { get; set; }

    public long JobId { get; set; }

    public string JobName { get; set; }

    /// <summary>1 成功 0 失败</summary>
    public int Status { get; set; }

    public string Message { get; set; }

    public long ElapsedMs { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime? EndTime { get; set; }
}
