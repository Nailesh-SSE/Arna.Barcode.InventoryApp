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
    public int CategoryId { get; set; }

    [StringLength(100)]
    public string CategoryName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Unit is required")]
    public int UnitId { get;  set; }

    [StringLength(20)]
    public string Unit { get; set; }
    [Required(ErrorMessage = "Company is required")]
    public int MakeCompanyId { get; set; }
    [StringLength(100)]
    public string MakeCompany { get; set; } = string.Empty;

    public int SerialNumber { get; set; }

    public int ColourId { get; set; }
    public string ColourName { get; set; }
    public virtual Category Category { get; set; } = null!;
    public virtual ICollection<InwardItem> InwardItems { get; set; } = new List<InwardItem>();
}