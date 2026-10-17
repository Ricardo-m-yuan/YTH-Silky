using Microsoft.Extensions.Hosting;
using Silky.Platform.Identity.Host;

var hostBuilder = Host.CreateDefaultBuilder(args)
    .ConfigureSilkyGeneralHostDefaults();

// Windows 平台以 Windows 服务方式运行；其它平台（本地开发/Linux）忽略
if (OperatingSystem.IsWindows())
{
    hostBuilder = hostBuilder.UseWindowsService(options => options.ServiceName = "SilkyPlatform.Identity");
}

await hostBuilder.Build().RunAsync();
