using Nop.Core;

namespace Nop.Plugin.Shipping.Dexpress.Domain;

public class Town : BaseEntity
{
    public string Name { get; set; }
    public string DName { get; set; }
    public int CentarId { get; set; }
    public int MId { get; set; }
    public int PttNo { get; set; }
    public int O { get; set; }
    public string DeliveryDays { get; set; }
    public string CutOffPickupTime { get; set; }
}