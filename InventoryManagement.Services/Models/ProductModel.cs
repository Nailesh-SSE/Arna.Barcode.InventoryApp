using System.ComponentModel.DataAnnotations;

namespace InventoryManagement.Services.Models;

    public class ProductModel:CommonModel
    {
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "Product name is required")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    //[Required(ErrorMessage = "SKU is required")]
    [StringLength(50)]
    public string SKU { get; set; } = string.Empty;

    [Required(ErrorMessage ="CategoryName is Required")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a category")]
    public int CategoryId { get; set; }

    [StringLength(100)]
    public string CategoryName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Unit is required")]
    [StringLength(20)]
    public string Unit { get; set; } = "PCS";

    [StringLength(100)]
    public string MakeCompany { get; set; } = string.Empty;
}

