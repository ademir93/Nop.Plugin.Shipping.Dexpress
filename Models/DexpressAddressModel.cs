using Nop.Core.Domain.Common;
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
    
    public Address ToEntity(Address destination = null)
    {
        destination ??= new Address();
        
        destination.Id = Id;
        destination.FirstName = FirstName;
        destination.LastName = LastName;
        destination.Email = Email;
        destination.CountryId = CountryId == 0 ? null : CountryId;
        destination.StateProvinceId = StateProvinceId == 0 ? null : StateProvinceId;
        destination.County = County;
        destination.City = City;
        destination.Address1 = Address1;
        destination.ZipPostalCode = ZipPostalCode;
        destination.PhoneNumber = PhoneNumber;

        return destination;
    }
}