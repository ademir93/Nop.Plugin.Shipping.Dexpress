using Nop.Core.Domain.Shipping;
using Nop.Services.Shipping.Tracking;

namespace Nop.Plugin.Shipping.Dexpress;

public class DexpressShipmentTracker : IShipmentTracker
{
    public DexpressShipmentTracker()
    {
        
    }
    
    public Task<string> GetUrlAsync(string trackingNumber, Shipment shipment = null)
    {
        return Task.FromResult($"https://www.dex.com/track?&tracknum={trackingNumber}");
    }
    
    public async Task<IList<ShipmentStatusEvent>> GetShipmentEventsAsync(string trackingNumber, Shipment shipment = null)
    {
        return null;
    }
}