using InventoryManagement.Services.Models;

namespace InventoryManagement.Services.Interfaces;

public interface IPermissionService
{
    Task<List<UserFormPermissionModel>> GetUserPermissionsAsync(int userId);
    Task<List<FormMasterModel>> GetPermittedFormsAsync(int userId);
    void ClearUserPermissionCache(int userId);
}
