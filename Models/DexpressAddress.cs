using Nop.Web.Models.Customer;

namespace Nop.Plugin.Shipping.Dexpress.Models;

public record DexpressAddress : CustomerAddressEditModel
{
    public DexpressAddress()
    {
        Address = new DexpressAddressModel();
    }

    public new DexpressAddressModel Address { get; set; }
}