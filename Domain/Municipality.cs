using Nop.Core;

namespace Nop.Plugin.Shipping.Dexpress.Domain;

public class Municipality : BaseEntity
{
    public string Name { get; set; }
    public int PttNo { get; set; }
    public int O { get; set; }
}