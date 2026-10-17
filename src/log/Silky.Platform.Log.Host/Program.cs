using Microsoft.Extensions.Hosting;
using Silky.Platform.Log.Host;

var hostBuilder = Host.CreateDefaultBuilder(args)
    .ConfigureSilkyGeneralHostDefaults();

if (OperatingSystem.IsWindows())
{
    hostBuilder = hostBuilder.UseWindowsService(options => options.ServiceName = "SilkyPlatform.Log");
}

await hostBuilder.Build().RunAsync();
