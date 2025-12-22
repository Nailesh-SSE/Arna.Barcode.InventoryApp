using System.ComponentModel.DataAnnotations;

namespace InventoryManagement.Core.Entities;

public class Inward : Common
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string InwardNo { get; set; } = string.Empty;

    [Required]
    public DateTime InwardDate { get; set; } = DateTime.UtcNow.Date;

    [Required]
    public int ShipMentCompanyId { get; set; }

    public bool IsSalesReturn { get; set; } = false;    

    [StringLength(500)]
    public string? Remarks { get; set; }
    public int? CategoryId { get; set; }
    public virtual Category? Category { get; set; }
    public virtual Company ShipMentCompany { get; set; } = null!;
    public virtual ICollection<InwardItem> InwardItems { get; set; } = new List<InwardItem>();
    public virtual ICollection<InwardBarcodeItem> InwardBarcodeItems { get; set; } = new List<InwardBarcodeItem>();
}