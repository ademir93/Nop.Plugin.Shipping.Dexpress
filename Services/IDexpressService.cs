using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;
using Nop.Services.Shipping;

namespace Nop.Plugin.Shipping.Dexpress.Services;

public interface IDexpressService
{
    Task<GetShippingOptionResponse> GetRatesAsync(GetShippingOptionRequest shippingOptionRequest);
    Task<bool> SyncMunicipalityAsync();
    Task<bool> SyncTownsAsync();
    Task<bool> SyncStreetsAsync();
    Task<string> GetShippmentCodeAsync();
    Task<Shipment> CreateDexpressShipmentAsync(int orderId, int warehouseId, List<OrderItem> orderItems, List<Product> products);
    Task<bool> CheckIsOrderFlagByDexpress(IList<OrderNote> orderNotes);
}
