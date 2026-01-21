using InventoryManagement.Services.Models;

namespace InventoryManagement.Services.Interfaces;

public interface IPermissionService
{
    Task<List<UserFormPermissionModel>> GetUserPermissionsAsync(int userId);
    Task<List<UserFormPermissionModel>> GetClientPermissionsAsync(int userId);
    Task<List<FormMasterModel>> GetPermittedFormsAsync(int userId);
    Task<List<FormMasterModel>> GetClientPermittedFormsAsync(int userId);
    void ClearUserPermissionCache(int userId);
}
