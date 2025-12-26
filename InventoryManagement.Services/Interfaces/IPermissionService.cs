using InventoryManagement.Services.Models;

namespace InventoryManagement.Services.Interfaces;

public interface IPermissionService
{
    Task<List<UserFormPermissionModel>> GetUserPermissionsAsync(int userId);
    void ClearUserPermissionCache(int userId);
}
