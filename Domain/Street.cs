using System.ComponentModel.DataAnnotations.Schema;
using Nop.Core;

namespace Nop.Plugin.Shipping.Dexpress.Domain;

public class Street : BaseEntity
{
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public new int Id { get; set; }
    public string Name { get; set; }
    public int TId { get; set; }
    public bool Del { get; set; }
}