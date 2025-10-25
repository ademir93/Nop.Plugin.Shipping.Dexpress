using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Shipping.Dexpress.Models;

public record ConfigurationModel : BaseNopModel
{
    [NopResourceDisplayName("Plugins.Shipping.Dex.Fields.Username")]
    public string Username { get; set; }

    [NopResourceDisplayName("Plugins.Shipping.Dex.Fields.Password")]
    public string Password { get; set; }

    [NopResourceDisplayName("Plugins.Shipping.Dex.Fields.ApiUrl")]
    public string ApiUrl { get; set; }

    [NopResourceDisplayName("Plugins.Shipping.Dex.Fields.Datetime")]
    public string Datetime { get; set; }
}