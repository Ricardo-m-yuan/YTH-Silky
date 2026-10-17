using Silky.Platform.Core.Dtos;

namespace Silky.Platform.Job.Application.Contracts.Jobs;

/// <summary>任务状态</summary>
public static class JobStatus
{
    /// <summary>已停止</summary>
    public const int Stopped = 0;

    /// <summary>运行中</summary>
    public const int Running = 1;
}

public class JobInfoDto
{
    public long Id { get; set; }

    public string Name { get; set; }

    /// <summary>任务分组，对应 Quartz 的 JobKey.Group</summary>
    public string Group { get; set; }

    /// <summary>CRON 表达式</summary>
    public string Cron { get; set; }

    /// <summary>执行器类型（实现 Quartz.IJob 的类的全名）</summary>
    public string JobType { get; set; }

    /// <summary>执行参数（JSON 字符串）</summary>
    public string JobData { get; set; }

    public int Status { get; set; }

    public string Description { get; set; }

    public DateTime? LastRunTime { get; set; }

    public DateTime? NextRunTime { get; set; }

    public DateTime CreatedTime { get; set; }
}

public class GetJobPageInput : PagedQueryInput
{
    public string Group { get; set; }

    public int? Status { get; set; }
}

public class CreateJobInput
{
    public string Name { get; set; }

    public string Group { get; set; } = "default";

    public string Cron { get; set; }

    /// <summary>执行器类型全名，如 Silky.Platform.Job.Application.Jobs.HttpCallJob</summary>
    public string JobType { get; set; }

    public string JobData { get; set; }

    public int Status { get; set; } = JobStatus.Running;

    public string Description { get; set; }
}

public class UpdateJobInput
{
    public long Id { get; set; }

    public string Name { get; set; }

    public string Group { get; set; }

    public string Cron { get; set; }

    public string JobType { get; set; }

    public string JobData { get; set; }

    public string Description { get; set; }
}

public class SetJobStatusInput
{
    public long Id { get; set; }

    public int Status { get; set; }
}

/// <summary>立即执行入参（保持为复杂类型，使路由推导为 POST）</summary>
public class TriggerJobInput
{
    public long Id { get; set; }
}

public class JobLogDto
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

public class GetJobLogPageInput : PagedQueryInput
{
    public long? JobId { get; set; }

    public int? Status { get; set; }
}

/// <summary>清理日志入参</summary>
public class ClearJobLogInput
{
    public long JobId { get; set; }
}
