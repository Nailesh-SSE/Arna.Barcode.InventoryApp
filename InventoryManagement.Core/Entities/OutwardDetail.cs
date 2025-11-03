using System.ComponentModel.DataAnnotations;

namespace InventoryManagement.Core.Entities;

public class OutwardDetail : Common
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int OutwardId { get; set; }
    public int ProductId { get; set; }

    [Required]
    [StringLength(100)]
    public string BarcodeNo { get; set; } = string.Empty;

    [Required]
    public int Quantity { get; set; } = 1;

    [Required]
    [StringLength(20)]
    public string Unit { get; set; } = "PCS";
    public virtual Product Product { get; set; }
    public virtual Outward Outward { get; set; } = null!;
}