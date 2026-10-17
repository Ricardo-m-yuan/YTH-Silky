namespace Silky.Platform.Log.Application.Entities;

public class OperationLog
{
    public long Id { get; set; }
    public long? UserId { get; set; }
    public string UserName { get; set; }
    public string ServiceName { get; set; }
    public string ApiPath { get; set; }
    public string HttpMethod { get; set; }
    public string Ip { get; set; }
    public int StatusCode { get; set; }
    public long ElapsedMs { get; set; }
    public string RequestBody { get; set; }
    public string ResponseBody { get; set; }
    public DateTime OperateTime { get; set; }
}

public class LoginLog
{
    public long Id { get; set; }
    public long? UserId { get; set; }
    public string UserName { get; set; }
    public string Ip { get; set; }
    public string Browser { get; set; }
    public string Os { get; set; }
    public int Status { get; set; }
    public string Message { get; set; }
    public DateTime LoginTime { get; set; }
}

public class ExceptionLog
{
    public long Id { get; set; }
    public long? UserId { get; set; }
    public string UserName { get; set; }
    public string ServiceName { get; set; }
    public string ApiPath { get; set; }
    public string HttpMethod { get; set; }
    public string Ip { get; set; }
    public string ExceptionType { get; set; }
    public string Message { get; set; }
    public string StackTrace { get; set; }
    public DateTime OccurTime { get; set; }
}
