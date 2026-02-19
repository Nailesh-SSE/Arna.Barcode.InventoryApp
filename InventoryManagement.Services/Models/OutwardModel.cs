using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryManagement.Services.Models;

public class OutwardModel:CommonModel
{
    public int Id { get; set; }
    public string OutwardNo { get; set; } = string.Empty;
    public DateTime OutwardDate { get; set; } = DateTime.UtcNow;
    public int BillToCompanyId { get; set; }
    public int PlatformId { get; set; }
    public string? Remarks { get; set; }
    public List<OutWardItemModel> OutwardItems { get; set; } = new();
    public bool IsFinished { get; set; } = false;
    public DateOnly OutwardDateOnly => DateOnly.FromDateTime(OutwardDate);
    public string BillToCompanyName { get; set; } = string.Empty;
    public string platformName { get; set; } = string.Empty;
}

public class OutWardItemModel
{
    public int Id { get; set; }
    public int OutwardId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Barcode is required")]
    public string BarcodeNo { get; set; } = string.Empty;

    public decimal Quantity { get; set; } = 1;
    public string Unit { get; set; } = "PCS";
}

public class BarcodeValidationResult
{
    public bool IsValid { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}
