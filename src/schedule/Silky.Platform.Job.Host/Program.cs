using Microsoft.Extensions.Hosting;
using Silky.Platform.Job.Host;

var hostBuilder = Host.CreateDefaultBuilder(args)
    .ConfigureSilkyGeneralHostDefaults();

if (OperatingSystem.IsWindows())
{
    hostBuilder = hostBuilder.UseWindowsService(options => options.ServiceName = "SilkyPlatform.Job");
}

await hostBuilder.Build().RunAsync();
