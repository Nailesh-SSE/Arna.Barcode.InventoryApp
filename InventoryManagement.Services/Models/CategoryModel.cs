using InventoryManagement.Core.Entities;
using System.ComponentModel.DataAnnotations;

namespace InventoryManagement.Services.Models;

public class CategoryModel : CommonModel
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "Category name is required")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Maximum 500 characters are allowed")]
    public string Description { get; set; } = string.Empty;

    public int? ParentCategoryId { get; set; }

    [StringLength(100, ErrorMessage = "Maximum 100 characters are allowed")]
    public string? ParentCategoryName { get; set; }

}
