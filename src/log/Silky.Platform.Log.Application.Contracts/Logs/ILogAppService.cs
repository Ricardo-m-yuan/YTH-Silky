using Silky.Platform.Core.Dtos;
using Silky.Rpc.Routing;

namespace Silky.Platform.Log.Application.Contracts.Logs;

/// <summary>
/// 日志管理：操作日志/登录日志/异常日志查询与清理。
/// </summary>
[ServiceRoute]
public interface ILogAppService
{
    Task<PagedResult<OperationLogDto>> GetOperationLogPageAsync(GetOperationLogInput input);

    Task<PagedResult<LoginLogDto>> GetLoginLogPageAsync(GetLoginLogInput input);

    Task<PagedResult<ExceptionLogDto>> GetExceptionLogPageAsync(GetExceptionLogInput input);

    Task ClearOperationLogAsync();

    Task ClearLoginLogAsync();

    Task ClearExceptionLogAsync();
}
