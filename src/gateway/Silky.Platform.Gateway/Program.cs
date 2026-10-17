using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Silky.Platform.Gateway;

var hostBuilder = Host.CreateDefaultBuilder(args)
    .ConfigureSilkyGatewayDefaults(webBuilder =>
    {
        webBuilder.UseStartup<Startup>();
    });

if (OperatingSystem.IsWindows())
{
    hostBuilder = hostBuilder.UseWindowsService(options => options.ServiceName = "SilkyPlatform.Gateway");
}

await hostBuilder.Build().RunAsync();
