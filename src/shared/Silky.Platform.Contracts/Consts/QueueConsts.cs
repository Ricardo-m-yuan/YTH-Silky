namespace Silky.Platform.Contracts.Consts;

/// <summary>
/// RabbitMQ 队列名（MassTransit 直接以队列名作为接收端点名）
/// </summary>
public static class QueueConsts
{
    public const string OperationLogQueue = "platform.log.operation";

    public const string LoginLogQueue = "platform.log.login";

    public const string ExceptionLogQueue = "platform.log.exception";
}
