using InventoryManagement.Core.Entities;
using InventoryManagement.Services.Models;

namespace InventoryManagement.Services.Interfaces;

public interface IFormPermissionService
{
    Task<List<FormPermissionModel>> GetAllAsync(int userRoleId);
    Task<List<FormMaster>> GetAllFormsAsync();
    Task<List<Roles>> GetAllRolesAsync();

    Task<bool> CreateAsync(FormPermissionModel model);
    Task<bool> UpdateAsync(FormPermissionModel model);
    Task<bool> DeleteAsync(int id, int deletedBy);

    Task<bool> ExistsAsync(int roleId, int formId, int? id = null);
    Task<bool> CheckDuplicate(int roleId, int formId, int? ignoreId=null);
}
