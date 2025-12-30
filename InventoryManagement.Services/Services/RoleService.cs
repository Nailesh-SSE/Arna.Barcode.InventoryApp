using InventoryManagement.Core.Entities;
using InventoryManagement.Infrastructure.Repositories;
using InventoryManagement.Services.Interfaces;
using InventoryManagement.Services.Models;

namespace InventoryManagement.Services.Services;

public class RoleService : IRoleService
{
    private readonly IUnitOfWork _unitOfWork;
    public RoleService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<RoleModel>> GetAllRolesAsync(int userRoleId)
    {
        try
        {
            var roleRepository = _unitOfWork.GetRepository<Roles>();

            var userRole = await roleRepository.GetByIdAsync(userRoleId);
            var isAdmin = userRole != null && userRole.RoleLevel < 5;

            IEnumerable<Roles> rolesEnumerable;
            if (isAdmin)
            {
                rolesEnumerable = await roleRepository.FindAsync(r => !r.IsDeleted && r.IsActive);
            }
            else
            {
                rolesEnumerable = await roleRepository.FindAsync(r => !r.IsDeleted && r.IsActive && r.RoleLevel > 5);
            }

            var roles = rolesEnumerable.ToList();

            return roles.Select(r => new RoleModel
            {
                Id = r.Id,
                Name = r.Name,
                RoleLevel = r.RoleLevel,
                Description = r.Remark,
                IsActive = r.IsActive,
                CreatedBy = r.CreatedBy,
                CreatedOn = r.CreatedOn,
                UpdatedBy = r.UpdatedBy,
                UpdatedOn = r.UpdatedOn,
                IsDeleted = r.IsDeleted
            }).ToList();
        }
        catch (Exception ex)
        {

            throw;
        }

    }

    public async Task<RoleModel?> GetRoleByIdAync(int userRoleId)
    {
        try
        {
            var roleRepository = _unitOfWork.GetRepository<Roles>();
            var role = await roleRepository.GetByIdAsync(userRoleId);
            if (role == null) return null;
            return new RoleModel
            {
                Id = role.Id,
                Name = role.Name,
                RoleLevel = role.RoleLevel,
                IsActive = role.IsActive,
                CreatedBy = role.CreatedBy,
                CreatedOn = role.CreatedOn,
                UpdatedBy = role.UpdatedBy,
                UpdatedOn = role.UpdatedOn,
                IsDeleted = role.IsDeleted
            };
        }
        catch (Exception ex)
        {

            throw;
        }

    }

    public async Task<bool> CreateRoleAsync(RoleModel roleModel)
    {
        var RoleRepository = _unitOfWork.GetRepository<Roles>();
        var roleList = await RoleRepository.GetAllAsync();
        int maxRoleLevel = roleList.Any() ? roleList.Max(x => x.RoleLevel) : 0;
        int nextRoleLevel = maxRoleLevel >= 5 ? maxRoleLevel + 1 : roleModel.RoleLevel;

        var Role = new Roles
        {
            Id = roleModel.Id,
            Name = roleModel.Name,
            RoleLevel = roleModel.RoleLevel > 0 ? roleModel.RoleLevel : nextRoleLevel,
            Remark = roleModel.Description,
            IsActive = true,
            IsDeleted = false,
            CreatedBy = roleModel.CreatedBy,
            CreatedOn = DateTime.UtcNow
        };
        await RoleRepository.AddAsync(Role);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }
    public async Task<bool> UpdateRoleAsync(RoleModel roleModel)
    {
        var roleRepository = _unitOfWork.GetRepository<Roles>();
        var Role = await roleRepository.GetByIdAsync(roleModel.Id);

        if (Role == null)
            return false;

        Role.Id = roleModel.Id;
        Role.Name = roleModel.Name;
        Role.RoleLevel = roleModel.RoleLevel;
        Role.Remark = roleModel.Description;
        Role.IsActive = roleModel.IsActive;
        Role.IsDeleted = roleModel.IsDeleted;
        Role.UpdatedBy = roleModel.UpdatedBy;
        Role.UpdatedOn = DateTime.UtcNow;

        roleRepository.Update(Role);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }
    public async Task<bool> DeleteRoleAsync(int id, int deletedBy)
    {
        try
        {
            var roleRepository = _unitOfWork.GetRepository<Roles>();
            var Role = await roleRepository.GetByIdAsync(id);
            if (Role == null) return false;
            Role.IsDeleted = true;
            Role.IsActive = false;
            Role.UpdatedOn = DateTime.UtcNow;
            Role.UpdatedBy = deletedBy;
            roleRepository.Update(Role);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }
    public async Task<bool> IsRoleNameUnique(string Name, int? id = null)
    {
        var roleRepository = _unitOfWork.GetRepository<Roles>();

        var Roles = await roleRepository.FindAsync(c => c.Name.ToLower() == Name.Trim().ToLower() && !c.IsDeleted);
        if (id.HasValue)
        {
            Roles = Roles.Where(c => c.Id != id.Value);
        }
        return !Roles.Any();
    }
    public async Task<int> GetRoleLevelByUserRoleId(int roleId)
    {
        var roleRepo = _unitOfWork.GetRepository<Roles>();

        var role = (await roleRepo
            .FindAsync(r => r.Id == roleId && !r.IsDeleted))
            .FirstOrDefault();

        if (role == null)
            return 0; // or throw exception

        return role.RoleLevel;
    }
}

