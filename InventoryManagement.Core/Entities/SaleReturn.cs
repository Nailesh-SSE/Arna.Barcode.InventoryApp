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

    public int BoxQuantity { get; set; } = 0;
    [Required]
    public int UnitId { get; set; } 

    [StringLength(500)]
    public string? Reason { get; set; }

    [StringLength(500)]
    public string? Remarks { get; set; }

    [Required]
    public DateTime BillingDate { get; set; }

    [Required]
    public int ProductId { get; set; }

    public bool IsTakeInStock { get; set; }        
    public int? ReturnInwardItemId { get; set; }     
   
    public virtual Company BillToCompany { get; set; } = null!;

    public virtual Product Product { get; set; }
}