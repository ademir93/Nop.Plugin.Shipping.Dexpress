using System.ComponentModel.DataAnnotations.Schema;
using Nop.Core;

namespace Nop.Plugin.Shipping.Dexpress.Domain;

public class Municipality : BaseEntity
{
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public new int Id { get; set; }
    public string Name { get; set; }
    public int PttNo { get; set; }
    public int O { get; set; }
}