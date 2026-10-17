using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Silky.Http.Core;
using Silky.Platform.Gateway.Authorization;

namespace Silky.Platform.Gateway;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSilkyHttpCore()
            .AddSwaggerDocuments()
            .AddRouting()
            .AddDashboard();

        // JWT 认证 + 授权（注入自定义权限处理器）
        services.AddSilkyIdentity<GatewayPermissionHandler>();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseSwaggerDocuments();
        }

        app.UseDashboard();
        app.UseRouting();
        app.UseSilkyIdentity();
        app.UseWebSockets();
        app.UseSilkyWebSocketsProxy();

        app.UseEndpoints(endpoints => { endpoints.MapSilkyRpcServices(); });
    }
}
