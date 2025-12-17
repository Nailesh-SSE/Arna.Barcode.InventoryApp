using System.ComponentModel.DataAnnotations;

namespace InventoryManagement.Core.Entities;

public class Roles : Common
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;
}
