using Nop.Services.Shipping;

namespace Nop.Plugin.Shipping.Dexpress.Services;

public interface IDexpressService
{
    Task<GetShippingOptionResponse> GetRatesAsync(GetShippingOptionRequest shippingOptionRequest);
    Task<bool> SyncMunicipalityAsync();
    Task<bool> SyncTownsAsync();
    Task<bool> SyncStreetsAsync();
}
