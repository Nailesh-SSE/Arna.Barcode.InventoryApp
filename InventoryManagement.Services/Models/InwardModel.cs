using System.ComponentModel.DataAnnotations;

namespace InventoryManagement.Services.Models;

public class InwardModel
{
    public int Id { get; set; }
    public string? InwardNo { get; set; }

    [Required(ErrorMessage = "Inward Date is required")]
    public DateTime InwardDate { get; set; }
    [Required(ErrorMessage = "Inward Time is required")]
    public TimeSpan InwardTime { get; set; }

    [Required(ErrorMessage = "Shipment Company is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Shipment Company is required")]
    public int ShipMentCompanyId { get; set; }
    public int? CategoryId { get; set; }
    public string? Remarks { get; set; }
    public bool IsActive { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedOn { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime? UpdatedOn { get; set; }
    public List<InwardItemModel> InwardItems { get; set; } = new();
}

public class InwardItemModel
{
    public int Id { get; set; }
    public int InwardId { get; set; }
    public int ProductId { get; set; }
    public decimal ItemQuantity { get; set; }
    public string? BatchNo { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedOn { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime? UpdatedOn { get; set; }
    public int InwardUnitId { get; set; }
    public string? InwardUnitName { get; set; }
    public decimal BoxQuantity { get; set; }
}
