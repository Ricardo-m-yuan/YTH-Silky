using Microsoft.Extensions.Hosting;

var hostBuilder = Host.CreateDefaultBuilder(args)
    .ConfigureSilkyGeneralHostDefaults();

if (OperatingSystem.IsWindows())
{
    hostBuilder = hostBuilder.UseWindowsService(options => options.ServiceName = "SilkyPlatform.Api");
}

await hostBuilder.Build().RunAsync();
