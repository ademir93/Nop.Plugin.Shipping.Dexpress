using Nop.Core.Configuration;

namespace Nop.Plugin.Shipping.Dexpress;

public class DexpressSettings : ISettings
{
    public string Username { get; set; }

    public string Password { get; set; }

    public string ApiUrl { get; set; }

    public string Datetime { get; set; }
}