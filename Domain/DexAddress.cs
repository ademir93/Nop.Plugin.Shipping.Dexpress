using Nop.Core;

namespace Nop.Plugin.Shipping.Dexpress.Domain;

public class DexAddress : BaseEntity
{
    public int AddressId { get; set; }
    public int MunicipalityId { get; set; }
    public int TownId { get; set; }
    public int StreetId { get; set; }
}