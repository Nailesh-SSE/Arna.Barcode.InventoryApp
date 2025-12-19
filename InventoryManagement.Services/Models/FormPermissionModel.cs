using System.ComponentModel.DataAnnotations;

namespace InventoryManagement.Services.Models;

public class FormPermissionModel:CommonModel
{
    public int Id { get; set; }  
    public int RoleId { get; set; }
    public int FormId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string FormName { get; set; } = string.Empty;

    public bool CanView { get; set; }
    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
}
