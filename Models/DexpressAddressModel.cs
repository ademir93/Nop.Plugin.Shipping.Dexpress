using Nop.Plugin.Shipping.Dexpress.Domain;
using Nop.Web.Models.Common;

namespace Nop.Plugin.Shipping.Dexpress.Models;

public record DexpressAddressModel : AddressModel
{
    
    public IList<Municipality> Municipalities { get; set; }
    public List<Town> Towns { get; set; }
    public List<Street> Streets { get; set; }
    
    public int MunicipalityId { get; set; }
    public int TownId { get; set; }
    public int StreetId { get; set; }
}