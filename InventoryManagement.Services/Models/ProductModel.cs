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

    //[Required(ErrorMessage ="CategoryName is Required")]
    //[Range(1, int.MaxValue, ErrorMessage = "Please select a category")]
    public int CategoryId { get; set; }

    [StringLength(100)]
    public string CategoryName { get; set; } = string.Empty;
    //[Required(ErrorMessage = "Unit is required")]
    //[Range(1, int.MaxValue, ErrorMessage = "Please select a unit")]
    public int UnitId { get; set; }

    [StringLength(20)]
    public string Unit { get; set; }
    //[Required(ErrorMessage = "Company is required")]
    //[Range(1, int.MaxValue, ErrorMessage = "Please select a Company")]
    public int MakeCompanyId { get; set; }
    [StringLength(100)]
    public string MakeCompany { get; set; } = string.Empty;

    public int SerialNumber { get; set; }
}

