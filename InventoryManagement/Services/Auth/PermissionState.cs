using InventoryManagement.Services.Models;

namespace InventoryManagement.Services.Auth;

public class PermissionState
{
    public List<UserFormPermissionModel> Permissions { get; private set; } = new();
    public bool IsLoaded { get; private set; }

    public void SetPermissions(List<UserFormPermissionModel> permissions)
    {
        Permissions = permissions;
        IsLoaded = true;
    }

    public void Clear()
    {
        Permissions.Clear();
        IsLoaded = false;
    }
}
