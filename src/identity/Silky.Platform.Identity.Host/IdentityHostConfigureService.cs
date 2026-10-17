using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Silky.Platform.Identity.Application.EntityFrameworkCore;

namespace Silky.Platform.Identity.Host;

/// <summary>
/// Silky 服务配置扩展点：框架启动时自动扫描并调用
/// </summary>
public class IdentityHostConfigureService : IConfigureService
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // JWT 编解码器与令牌生成器
        services.AddJwt();

        // EF Core + SQL Server（原生 EF Core，不用 Silky.EntityFrameworkCore 封装）
        services.AddDbContext<IdentityDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("default"),
                sql => sql.MigrationsAssembly(typeof(IdentityHostConfigureService).Assembly.GetName().Name)));

        // MassTransit + RabbitMQ：发布日志事件
        services.AddMassTransit(x =>
        {
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

                cfg.ConfigureEndpoints(context);
            });
        });
    }
}
