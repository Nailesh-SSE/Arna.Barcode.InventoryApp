using InventoryManagement.Services.Models;

namespace InventoryManagement.Services.Interfaces;

public interface IPermissionService
{
    Task<List<UserFormPermissionModel>> GetUserPermissionsAsync(int userId, CancellationToken cancellationToken = default);
    Task<List<FormMasterModel>> GetPermittedFormsAsync(int userId, CancellationToken cancellationToken = default);
    void ClearUserPermissionCache(int userId);
}
