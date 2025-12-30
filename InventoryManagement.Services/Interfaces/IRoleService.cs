using InventoryManagement.Services.Models;

namespace InventoryManagement.Services.Interfaces
{
    public interface IRoleService
    {
        Task<List<RoleModel>> GetAllRolesAsync(int userRoleId);
        Task<RoleModel> GetRoleByIdAync(int userRoleId);
        Task<bool> CreateRoleAsync(RoleModel roleModel);
        Task<bool> UpdateRoleAsync(RoleModel roleModel);
        Task<bool> DeleteRoleAsync(int id, int deletedBy);
        Task<bool> IsRoleNameUnique(string Name, int? id = null);
        Task<int> GetRoleLevelByUserRoleId(int roleId);
    }
}
