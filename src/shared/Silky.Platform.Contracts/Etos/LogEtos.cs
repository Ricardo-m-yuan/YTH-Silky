namespace Silky.Platform.Contracts.Etos;

/// <summary>
/// 操作日志事件：业务服务发布，日志服务消费后落库
/// </summary>
public class OperationLogEto
{
    public long? UserId { get; set; }

    public string UserName { get; set; }

    /// <summary>服务名，如 Silky.Platform.Identity</summary>
    public string ServiceName { get; set; }

    /// <summary>接口路由，如 api/user</summary>
    public string ApiPath { get; set; }

    public string HttpMethod { get; set; }

    public string Ip { get; set; }

    public int StatusCode { get; set; }

    /// <summary>耗时（毫秒）</summary>
    public long ElapsedMs { get; set; }

    public string RequestBody { get; set; }

    public string ResponseBody { get; set; }

    public DateTime OperateTime { get; set; } = DateTime.Now;
}

/// <summary>
/// 登录日志事件
/// </summary>
public class LoginLogEto
{
    public long? UserId { get; set; }

    public string UserName { get; set; }

    public string Ip { get; set; }

    public string Browser { get; set; }

    public string Os { get; set; }

    /// <summary>1 成功 0 失败</summary>
    public int Status { get; set; }

    public string Message { get; set; }

    public DateTime LoginTime { get; set; } = DateTime.Now;
}

/// <summary>
/// 异常日志事件
/// </summary>
public class ExceptionLogEto
{
    public long? UserId { get; set; }

    public string UserName { get; set; }

    public string ServiceName { get; set; }

    public string ApiPath { get; set; }

    public string HttpMethod { get; set; }

    public string Ip { get; set; }

    public string ExceptionType { get; set; }

    public string Message { get; set; }

    public string StackTrace { get; set; }

    public DateTime OccurTime { get; set; } = DateTime.Now;
}
