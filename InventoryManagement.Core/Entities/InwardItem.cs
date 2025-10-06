using System.ComponentModel.DataAnnotations;

namespace InventoryManagement.Core.Entities;

public class InwardItem : Common
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int InwardId { get; set; }

    [Required]
    public int ProductId { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
    public decimal Quantity { get; set; }

    [Required]
    [StringLength(20)]
    public string Unit { get; set; } = "PCS";

    [Required(ErrorMessage = "Batch number is required")]
    [StringLength(50)]
    public string BatchNo { get; set; } = string.Empty;

    [StringLength(50)]
    public string? SerialNo { get; set; }

    [Required]
    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

    public virtual Inward Inward { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
    public virtual ICollection<InwardBarcodeItem> InwardBarcodeItems { get; set; } = new List<InwardBarcodeItem>();
}