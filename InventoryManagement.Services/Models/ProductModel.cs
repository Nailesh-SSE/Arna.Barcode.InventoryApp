using System.ComponentModel.DataAnnotations;

namespace InventoryManagement.Services.Models;

public class ProductModel : CommonModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [StringLength(50)]
    public string SKU { get; set; } = string.Empty;
    public int CategoryId { get; set; }

    [StringLength(100)]
    public string CategoryName { get; set; } = string.Empty;
    public int UnitId { get; set; }

    [StringLength(20)]
    public string Unit { get; set; }
    public int MakeCompanyId { get; set; }
    [StringLength(100)]
    public string MakeCompany { get; set; } = string.Empty;

    public int SerialNumber { get; set; }
    public string? ImagePath { get; set; }
    public int ColourId { get; set; }
    public string ColourName { get; set; }
    public string ColourSearchText { get; set; } = string.Empty;
}
