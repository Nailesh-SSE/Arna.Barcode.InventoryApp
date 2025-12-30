using System.ComponentModel.DataAnnotations;

namespace InventoryManagement.Core.Entities;

public class RoleFormPermission : Common
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int RoleId { get; set; }

    [Required]
    public int FormId { get; set; }

    public bool CanView { get; set; }
    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
}

