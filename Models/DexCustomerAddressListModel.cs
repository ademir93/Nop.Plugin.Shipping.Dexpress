using Nop.Web.Framework.Models;

namespace Nop.Plugin.Shipping.Dexpress.Models;

public partial record DexCustomerAddressListModel : BaseNopModel
{
    public DexCustomerAddressListModel()
    {
        Addresses = new List<DexpressAddressModel>();
    }

    public IList<DexpressAddressModel> Addresses { get; set; }
}