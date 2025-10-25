using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core.Infrastructure;
using Nop.Plugin.Shipping.Dexpress.Services;

namespace Nop.Plugin.Shipping.Dexpress.Infrastructure;

public class NopStartup : INopStartup
{
    public virtual void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IDexpressService, DexpressService>();
    }

    public void Configure(IApplicationBuilder application)
    {
    }

    public int Order => 3000;
}