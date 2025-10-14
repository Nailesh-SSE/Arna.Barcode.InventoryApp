using System.ComponentModel.DataAnnotations;

namespace InventoryManagement.Core.Entities;

public class Product : Common
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "Product name is required")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "SKU is required")]
    [StringLength(50)]
    public string SKU { get; set; } = string.Empty;

    [Required(ErrorMessage ="Category is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a category")]
    public int CategoryId { get; set; }

    [StringLength(100)]
    public string CategoryName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Unit is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a unit")]
    public int UnitId { get;  set; }

    [StringLength(20)]
    public string Unit { get; set; }
    [Required(ErrorMessage = "Company is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a Company")]
    public int MakeCompanyId { get; set; }
    [StringLength(100)]
    public string MakeCompany { get; set; } = string.Empty;

    public virtual Category Category { get; set; } = null!;
    public virtual ICollection<InwardItem> InwardItems { get; set; } = new List<InwardItem>();
}