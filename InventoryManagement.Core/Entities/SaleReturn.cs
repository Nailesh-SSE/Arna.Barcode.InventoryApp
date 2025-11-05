using System.ComponentModel.DataAnnotations;
using InventoryManagement.Core.Enums;

namespace InventoryManagement.Core.Entities;

public class SaleReturn : Common
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string ReturnNo { get; set; } = string.Empty;

    [Required]
    public DateTime ReturnDate { get; set; } = DateTime.UtcNow.Date;

    [Required]
    public int BillToCompanyId { get; set; }

    [Required]
    public ReturnType ReturnType { get; set; }

    [StringLength(100)]
    public string BarcodeNo { get; set; } = string.Empty;

    [Required]
    public int Quantity { get; set; } = 1;

    [StringLength(500)]
    public string? Reason { get; set; }

    [StringLength(500)]
    public string? Remarks { get; set; }

    public virtual Company BillToCompany { get; set; } = null!;
}