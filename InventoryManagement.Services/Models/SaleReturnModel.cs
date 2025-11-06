using System.ComponentModel.DataAnnotations;
using InventoryManagement.Core.Enums;

namespace InventoryManagement.Services.Models;

public class SaleReturnModel : CommonModel
{
    public int Id { get; set; }

    [Required]
    public string ReturnNo { get; set; } = string.Empty;

    [Required]
    public DateTime ReturnDate { get; set; } = DateTime.UtcNow.Date;

    [Required]
    public int BillToCompanyId { get; set; }

    [Required]
    public ReturnType? ReturnType { get; set; }

    [Required(ErrorMessage = "Barcode is required")]
    public string BarcodeNo { get; set; } = string.Empty;

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
    public int Quantity { get; set; } = 1;

    public string? Reason { get; set; }
    public string? Remarks { get; set; }

    // Navigation properties for UI
    public string CompanyName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    [Required]
    public DateTime BillingDate { get; set; }
    [Required(ErrorMessage = "Product is required")]
    public int ProductId { get; set; } // new
}

public class SaleReturnValidationResult
{
    public bool IsValid { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
}