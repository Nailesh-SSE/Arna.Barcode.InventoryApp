using System.ComponentModel.DataAnnotations;

namespace InventoryManagement.Services.Models;

public class OutwardModel:CommonModel
{
    public int Id { get; set; }
    public string OutwardNo { get; set; } = string.Empty;
    public DateTime OutwardDate { get; set; } = DateTime.UtcNow.Date;
    public int BillToCompanyId { get; set; }
    public string? Remarks { get; set; }
    public List<OutWardItemModel> OutwardItems { get; set; } = new();
    public bool IsFinished { get; set; } = false;

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
