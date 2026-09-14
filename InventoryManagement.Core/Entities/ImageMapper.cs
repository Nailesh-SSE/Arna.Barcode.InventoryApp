using System.ComponentModel.DataAnnotations;

namespace InventoryManagement.Core.Entities;

public class ImageMapper : Common
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ItemId { get; set; }

    [Required]
    [StringLength(50)]
    public string Type { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string ImagePath { get; set; } = string.Empty;

    public int DisplayOrder { get; set; }
}
