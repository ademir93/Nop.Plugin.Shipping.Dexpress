using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;
using Nop.Plugin.Shipping.Dexpress.Domain;
using Nop.Plugin.Shipping.Dexpress.Models;
using Nop.Services.Shipping;

namespace Nop.Plugin.Shipping.Dexpress.Services;

public interface IDexpressService
{
    Task<GetShippingOptionResponse> GetRatesAsync(GetShippingOptionRequest shippingOptionRequest);
    Task<bool> SyncMunicipalityAsync();
    Task<bool> SyncTownsAsync();
    Task<bool> SyncStreetsAsync();
    Task<IList<Municipality>> GetAllMunicipalitiesAsync();
    Task<IList<Town>> GetTownsByMunicipalityIdAsync(int municipalityId);
    Task<IList<Street>> GetStreetsByTownIdAsync(int townId);
    Task<string> GetShippmentCodeAsync();
    Task<Shipment> CreateDexpressShipmentAsync(int orderId, int warehouseId, List<OrderItem> orderItems, List<Product> products);
    Task<bool> CheckIsOrderFlagByDexpress(IList<OrderNote> orderNotes);
    Task<DexpressOrder> GetDexpressOrderAsync(int orderId);
    Task<DexpressOrder> PostDexpressOrderAsync(DexpressOrder dexpressOrder);
    Task DeleteDexAddressAsync(DexAddress address);
    Task<DexAddress> GetDexAddressByIdAsync(int addressId);
    Task InsertDexAddressAsync(DexAddress address);
    Task UpdateDexAddressAsync(DexAddress address);
    Task<DexCustomerAddressListModel> PrepareCustomerAddressListModelAsync();
}
