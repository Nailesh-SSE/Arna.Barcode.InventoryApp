using System.ComponentModel.DataAnnotations;

namespace InventoryManagement.Core.Entities;

public class UsersInRole : Common
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int RoleId { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    public int CompanyId { get; set; }

    public virtual Users User { get; set; } = null!;
    public virtual Company Company { get; set; } = null!;
}