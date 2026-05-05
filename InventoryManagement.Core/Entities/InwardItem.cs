using System.ComponentModel.DataAnnotations;

namespace InventoryManagement.Core.Entities;

public class InwardItem : Common
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int InwardId { get; set; }
    [Required]
    public int BrandId { get; set; }
    [Required]
    public int ProductId { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Item Quantity must be greater than 0")]
    public decimal ItemQuantity { get; set; }

    [Required] 
    public int InwardUnitId { get; set; }

    [Required]
    [StringLength(20)]
    public string? InwardUnitName { get; set; }

    [Required(ErrorMessage = "Batch number is required")]
    [StringLength(50)]
    public string BatchNo { get; set; } = string.Empty;

    [StringLength(50)]
    public string? SerialNo { get; set; }

    [Required]
    public DateTime TransactionDate { get; set; } = DateTime.Now;

    public virtual Inward Inward { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;

    public decimal BoxQuantity { get; set; }
    public virtual ICollection<InwardBarcodeItem> InwardBarcodeItems { get; set; } = new List<InwardBarcodeItem>();
}