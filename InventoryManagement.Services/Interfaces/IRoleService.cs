using InventoryManagement.Core.Entities;
using InventoryManagement.Services.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InventoryManagement.Services.Interfaces
{
    public interface IRoleService
    {
        Task<List<RoleModel>> GetAllRolesAsync(int userRoleId);
        Task<bool> CreateRoleAsync(RoleModel roleModel);
        Task<bool> UpdateRoleAsync(RoleModel roleModel);
        Task<bool> DeleteRoleAsync(int id, int deletedBy);
        Task<bool> IsRoleNameUnique(string Name, int? id = null);
    }
}
