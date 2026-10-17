using Silky.Platform.Core.Dtos;
using Silky.Rpc.Routing;

namespace Silky.Platform.Job.Application.Contracts.Jobs;

/// <summary>
/// 定时任务管理：CRUD、启停、立即执行、执行日志。路由：api/job/*
/// </summary>
[ServiceRoute]
public interface IJobAppService
{
    Task<PagedResult<JobInfoDto>> GetPageAsync(GetJobPageInput input);

    Task<IReadOnlyList<JobInfoDto>> GetAllAsync();

    Task<JobInfoDto> GetAsync(long id);

    Task<JobInfoDto> CreateAsync(CreateJobInput input);

    Task<JobInfoDto> UpdateAsync(UpdateJobInput input);

    Task DeleteAsync(long id);

    /// <summary>启停任务（暂停/恢复 Quartz 触发器）</summary>
    Task SetStatusAsync(SetJobStatusInput input);

    /// <summary>立即执行一次</summary>
    Task TriggerAsync(TriggerJobInput input);

    Task<PagedResult<JobLogDto>> GetLogPageAsync(GetJobLogPageInput input);

    /// <summary>清空指定任务的执行日志</summary>
    Task ClearLogAsync(ClearJobLogInput input);
}
