using System.ComponentModel.DataAnnotations;

namespace InventoryManagement.Core.Entities;

public class OutwardDetail : Common
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int OutwardId { get; set; }

    [Required]
    public int InwardBarcodeItemId { get; set; }

    [Required(ErrorMessage = "Barcode number is required")]
    [StringLength(100)]
    public string BarcodeNo { get; set; } = string.Empty;

    [Required]
    public int Quantity { get; set; } = 1;

    [Required]
    [StringLength(20)]
    public string Unit { get; set; } = "PCS";

    public virtual Outward Outward { get; set; } = null!;
    public virtual InwardBarcodeItem InwardBarcodeItem { get; set; } = null!;
}