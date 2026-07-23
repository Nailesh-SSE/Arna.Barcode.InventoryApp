using System.ComponentModel.DataAnnotations;
namespace InventoryManagement.Core.Entities;

public class Platform : Common
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string Remark { get; set; } = string.Empty;

    public string? ImagePath { get; set; }
}
