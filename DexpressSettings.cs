using Nop.Core.Configuration;

namespace Nop.Plugin.Shipping.Dexpress;

public class DexpressSettings : ISettings
{
    public string CClientId { get; set; }
    public string Username { get; set; }

    public string Password { get; set; }

    public string ApiUrl { get; set; }

    public string Datetime { get; set; }
    
    public string Prefix { get; set; }
    
    public int RangeFrom { get; set; }
    
    public int RangeTo { get; set; }
}