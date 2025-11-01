using Nop.Core;

namespace Nop.Plugin.Shipping.Dexpress.Domain;

public class DexpressOrder : BaseEntity
{
    public int OrderId { get; set; }
    
    public DateTime OrderDate { get; set; }
}