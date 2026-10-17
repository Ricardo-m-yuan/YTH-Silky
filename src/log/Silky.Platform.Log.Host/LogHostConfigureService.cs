using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Silky.Platform.Contracts.Consts;
using Silky.Platform.Contracts.Etos;
using Silky.Platform.Log.Application.Consumers;
using Silky.Platform.Log.Application.EntityFrameworkCore;

namespace Silky.Platform.Log.Host;

public class LogHostConfigureService : IConfigureService
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<LogDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("default"),
                sql => sql.MigrationsAssembly(typeof(LogHostConfigureService).Assembly.GetName().Name)));

        // MassTransit + RabbitMQ：消费三种日志事件
        services.AddMassTransit(x =>
        {
            x.AddConsumer<OperationLogConsumer>();
            x.AddConsumer<LoginLogConsumer>();
            x.AddConsumer<ExceptionLogConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(configuration["rabbitMq:host"] ?? "127.0.0.1",
                    ushort.TryParse(configuration["rabbitMq:port"], out var port) ? port : (ushort)5672,
                    configuration["rabbitMq:virtualHost"] ?? "/",
                    h =>
                    {
                        h.Username(configuration["rabbitMq:userName"] ?? "guest");
                        h.Password(configuration["rabbitMq:password"] ?? "guest");
                    });

                cfg.ReceiveEndpoint(QueueConsts.OperationLogQueue, e =>
                {
                    e.ConfigureConsumer<OperationLogConsumer>(context);
                });
                cfg.ReceiveEndpoint(QueueConsts.LoginLogQueue, e =>
                {
                    e.ConfigureConsumer<LoginLogConsumer>(context);
                });
                cfg.ReceiveEndpoint(QueueConsts.ExceptionLogQueue, e =>
                {
                    e.ConfigureConsumer<ExceptionLogConsumer>(context);
                });
            });
        });
    }
}
