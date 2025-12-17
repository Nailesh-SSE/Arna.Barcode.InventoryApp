using System.ComponentModel.DataAnnotations;

namespace InventoryManagement.Services.Models;

public class FormPermissionModel
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Role is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Role is required")]
    public int RoleId { get; set; }

    [Required(ErrorMessage = "Form is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Form is required")]
    public int FormId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string FormName { get; set; } = string.Empty;

    public bool CanView { get; set; }
    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }

    public int CreatedBy { get; set; }
}
