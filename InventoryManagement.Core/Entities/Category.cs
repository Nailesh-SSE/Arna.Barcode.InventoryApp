using System.ComponentModel.DataAnnotations;

namespace InventoryManagement.Core.Entities;

public class Category : Common
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "Category name is required")]
    [StringLength(100,ErrorMessage ="Maximum 100 characters are allowed")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Maximum 500 characters are allowed")]
    public string Description { get; set; } = string.Empty;

    public int? ParentCategoryId { get; set; }

    [StringLength(100, ErrorMessage = "Maximum 100 characters are allowed")]
    public string? ParentCategoryName { get; set; }

    public virtual Category? ParentCategory { get; set; }
    public virtual ICollection<Category> SubCategories { get; set; } = new List<Category>();
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}