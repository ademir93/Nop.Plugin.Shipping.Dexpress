using Nop.Core;

namespace Nop.Plugin.Shipping.Dexpress.Domain;

public class Street : BaseEntity
{
    public string Name { get; set; }
    public int TId { get; set; }
    public bool Del { get; set; }
}