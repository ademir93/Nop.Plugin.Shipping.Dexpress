using System.ComponentModel.DataAnnotations.Schema;
using Nop.Core;

namespace Nop.Plugin.Shipping.Dexpress.Domain;

public class Street : BaseEntity
{
    public int SId { get; set; }
    public string Name { get; set; }
    public int TId { get; set; }
    public bool Del { get; set; }
}