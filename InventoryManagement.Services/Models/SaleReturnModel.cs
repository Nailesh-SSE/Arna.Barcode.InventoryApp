using System.ComponentModel.DataAnnotations;
using InventoryManagement.Core.Enums;

namespace InventoryManagement.Services.Models;

public class SaleReturnModel : CommonModel
{
    public int Id { get; set; }

    public string? ReturnNo { get; set; } = string.Empty;

    [Required]
    public DateTime ReturnDate { get; set; } = DateTime.UtcNow.Date;

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Bill To Company is required.")]
    public int BillToCompanyId { get; set; }

    [Required]
    public ReturnType? ReturnType { get; set; }

    public string? BarcodeNo { get; set; } = string.Empty;

    [Required]
    public int Quantity { get; set; }
    public int BoxQuantity { get; set; }   

    public string? Reason { get; set; }
    public string? Remarks { get; set; }

    public string? CompanyName { get; set; } = string.Empty;
    public string? ProductName { get; set; } = string.Empty;

    [Required]
    public DateTime? BillingDate { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Product is required")]
    public int ProductId { get; set; } // new

    public bool IsTakeInStock { get; set; }
    public int? ReturnInwardItemId { get; set; }
    [Required]
   // [Range(1, int.MaxValue, ErrorMessage = "Please select a valid unit")]
    public int UnitId { get; set; }
    public string? UnitName { get; set;} = string.Empty;
    public int CompanyId { get; set; }
}

public class SaleReturnValidationResult
{
    public bool IsValid { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
}