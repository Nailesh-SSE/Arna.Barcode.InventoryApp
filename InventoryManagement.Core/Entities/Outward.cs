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

    [Required]
    public int PlatformId { get; set; }

    [Required]
    public string LotNo { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Remarks { get; set; }
    public bool IsFinished { get; set; }=false;
    public virtual Company BillToCompany { get; set; } = null!;
    public virtual ICollection<OutwardDetail> OutwardDetails { get; set; } = new List<OutwardDetail>();

}