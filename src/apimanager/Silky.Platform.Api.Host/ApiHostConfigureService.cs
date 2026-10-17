using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Silky.Platform.Api.Application.EntityFrameworkCore;

namespace Silky.Platform.Api.Host;

public class ApiHostConfigureService : IConfigureService
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApiDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("default"),
                sql => sql.MigrationsAssembly(typeof(ApiHostConfigureService).Assembly.GetName().Name)));
    }
}
