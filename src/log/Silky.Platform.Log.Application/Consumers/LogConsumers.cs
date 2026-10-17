using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silky.Platform.Contracts.Consts;
using Silky.Platform.Contracts.Etos;
using Silky.Platform.Log.Application.EntityFrameworkCore;

namespace Silky.Platform.Log.Application.Consumers;

/// <summary>
/// 操作日志消费者：收到事件后落库。
/// </summary>
public class OperationLogConsumer : IConsumer<OperationLogEto>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OperationLogConsumer> _logger;

    public OperationLogConsumer(IServiceScopeFactory scopeFactory, ILogger<OperationLogConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OperationLogEto> context)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LogDbContext>();
        var eto = context.Message;

        db.OperationLogs.Add(new Entities.OperationLog
        {
            UserId = eto.UserId,
            UserName = eto.UserName,
            ServiceName = eto.ServiceName,
            ApiPath = eto.ApiPath,
            HttpMethod = eto.HttpMethod,
            Ip = eto.Ip,
            StatusCode = eto.StatusCode,
            ElapsedMs = eto.ElapsedMs,
            RequestBody = eto.RequestBody,
            ResponseBody = eto.ResponseBody,
            OperateTime = eto.OperateTime
        });

        await db.SaveChangesAsync(context.CancellationToken);
        _logger.LogDebug("OperationLog consumed: {Service}/{Api}", eto.ServiceName, eto.ApiPath);
    }
}

public class LoginLogConsumer : IConsumer<LoginLogEto>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LoginLogConsumer> _logger;

    public LoginLogConsumer(IServiceScopeFactory scopeFactory, ILogger<LoginLogConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<LoginLogEto> context)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LogDbContext>();
        var eto = context.Message;

        db.LoginLogs.Add(new Entities.LoginLog
        {
            UserId = eto.UserId,
            UserName = eto.UserName,
            Ip = eto.Ip,
            Browser = eto.Browser,
            Os = eto.Os,
            Status = eto.Status,
            Message = eto.Message,
            LoginTime = eto.LoginTime
        });

        await db.SaveChangesAsync(context.CancellationToken);
        _logger.LogDebug("LoginLog consumed: {User} status={Status}", eto.UserName, eto.Status);
    }
}

public class ExceptionLogConsumer : IConsumer<ExceptionLogEto>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExceptionLogConsumer> _logger;

    public ExceptionLogConsumer(IServiceScopeFactory scopeFactory, ILogger<ExceptionLogConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ExceptionLogEto> context)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LogDbContext>();
        var eto = context.Message;

        db.ExceptionLogs.Add(new Entities.ExceptionLog
        {
            UserId = eto.UserId,
            UserName = eto.UserName,
            ServiceName = eto.ServiceName,
            ApiPath = eto.ApiPath,
            HttpMethod = eto.HttpMethod,
            Ip = eto.Ip,
            ExceptionType = eto.ExceptionType,
            Message = eto.Message,
            StackTrace = eto.StackTrace,
            OccurTime = eto.OccurTime
        });

        await db.SaveChangesAsync(context.CancellationToken);
        _logger.LogError("ExceptionLog consumed: {Type} {Message}", eto.ExceptionType, eto.Message);
    }
}
