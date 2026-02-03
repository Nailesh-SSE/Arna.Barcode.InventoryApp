using InventoryManagement.Core.Entities;
using System.ComponentModel.DataAnnotations;

public class SaleReturnItems : Common
{
    [Key]
    public int Id { get; set; }
    [Required]
    public int SaleReturnId { get; set; }
    public int? OutwardId { get; set; } = null;
    [Required]
    public int ProductId { get; set; }
    [Required]
    public int SerialNo { get; set; }
    [StringLength(50)]
    public string? BarCodeNo { get; set; } = null;
    [Required]
    public int UnitId { get; set; }
    [Required]
    public decimal ReturnQuantity { get; set; } = 1;
    [Required]
    public int CategoryId { get; set; }
    [Required]
    public int ShipToCompanyId { get; set; } = 0;
    [Required]
    public int BillToCompanyId { get; set; } = 0;
    [Required]
    public int ReturnType { get; set; }
    public bool IsTakeInStock { get; set; } = false;
    [StringLength(200)]
    public string? ReasonToReturn { get; set; } = null;
    public DateTime? ReturnDate { get; set; } = DateTime.Now;

    public virtual Product Product { get; set; } = null!;
    public virtual Category Category { get; set; } = null!;
    public virtual Company ShipToCompany { get; set; } = null!;
    public virtual SaleReturn SaleReturn { get; set; } = null!;
}