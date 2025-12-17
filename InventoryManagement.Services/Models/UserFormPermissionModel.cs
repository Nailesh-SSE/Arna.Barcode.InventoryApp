namespace InventoryManagement.Services.Models;

public class UserFormPermissionModel
{
    public string Route { get; set; } = string.Empty;

    public bool CanView { get; set; }
    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
}
