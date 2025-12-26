using System.ComponentModel.DataAnnotations;

namespace InventoryManagement.Core.Entities;

public class Roles : Common
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;
    [StringLength(100)]
    public string? Remark { get; set; } = string.Empty;    

    public int RoleLevel { get; set; }
}
