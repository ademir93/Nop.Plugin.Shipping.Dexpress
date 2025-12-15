using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework.Mvc.Routing;

namespace Nop.Plugin.Shipping.Dexpress.Infrastructure;

public class RouteProvider : IRouteProvider
{
    public void RegisterRoutes(IEndpointRouteBuilder endpointRouteBuilder)
    {
        // Override the core GET/POST route
        endpointRouteBuilder.MapControllerRoute(
            name: "CustomerAddressAdd",
            pattern: "/customer/addressadd", // must include optional language
            defaults: new { controller = "DexpressCustomer", action = "AddressAdd" }
        );
    }
    
    public int Priority => 2; // must be higher than default routes
}