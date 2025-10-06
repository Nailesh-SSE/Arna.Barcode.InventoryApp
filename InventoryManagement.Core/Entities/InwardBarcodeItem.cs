using System.ComponentModel.DataAnnotations;

namespace InventoryManagement.Core.Entities;

public class InwardBarcodeItem : Common
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int InwardId { get; set; }

    [Required]
    public int InwardItemId { get; set; }

    [Required(ErrorMessage = "Barcode number is required")]
    [StringLength(100)]
    public string BarcodeNo { get; set; } = string.Empty;

    [Required]
    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

    public bool IsInStock { get; set; } = true;

    public virtual Inward Inward { get; set; } = null!;
    public virtual InwardItem InwardItem { get; set; } = null!;
    public virtual ICollection<OutwardDetail> OutwardDetails { get; set; } = new List<OutwardDetail>();
}