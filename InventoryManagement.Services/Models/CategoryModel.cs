using InventoryManagement.Core.Entities;
using System.ComponentModel.DataAnnotations;

namespace InventoryManagement.Services.Models;

public class CategoryModel : CommonModel
{
    public int Id { get; set; }

    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    public int? ParentCategoryId { get; set; }

    [StringLength(100)]
    public string? ParentCategoryName { get; set; }

    public int SerialNumber { get; set; }

}
