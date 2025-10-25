using Nop.Core;

namespace Nop.Plugin.Shipping.Dexpress;

public class DexpressDefaults
{
    public static string SystemName => "Shipping.Dexpress";
    
    public static string UserAgent => $"nopCommerce-{NopVersion.CURRENT_VERSION}";
    
    public static (string Name, string Type, int Period) SynchronizationTask =>
        ("Synchronization (Dexpress addresses)", "Nop.Plugin.Shipping.Dexpress.Services.DexpressSyncTask", 86400);
    
}