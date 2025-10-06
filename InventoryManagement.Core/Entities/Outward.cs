using System.ComponentModel.DataAnnotations;

namespace InventoryManagement.Core.Entities;

public class Outward : Common
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string OutwardNo { get; set; } = string.Empty;

    [Required]
    public DateTime OutwardDate { get; set; } = DateTime.UtcNow.Date;

    [Required]
    public int BillToCompanyId { get; set; }

    [StringLength(50)]
    public string? InvoiceNo { get; set; }

    public DateTime? InvoiceDate { get; set; }

    [StringLength(50)]
    public string? ChallanNo { get; set; }

    [StringLength(500)]
    public string? Remarks { get; set; }

    public virtual Company BillToCompany { get; set; } = null!;
    public virtual ICollection<OutwardDetail> OutwardDetails { get; set; } = new List<OutwardDetail>();
}