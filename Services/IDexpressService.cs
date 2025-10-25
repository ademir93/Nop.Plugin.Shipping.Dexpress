using Nop.Services.Shipping;

namespace Nop.Plugin.Shipping.Dexpress.Services;

public interface IDexpressService
{
    Task<GetShippingOptionResponse> GetRatesAsync(GetShippingOptionRequest shippingOptionRequest);
}
