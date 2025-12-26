using InventoryManagement.Core.Entities;
using InventoryManagement.Core.Enums;
using InventoryManagement.Infrastructure.Repositories;
using InventoryManagement.Services.Interfaces;
using InventoryManagement.Services.Models;
using Microsoft.Extensions.Caching.Memory;

namespace InventoryManagement.Services.Services;

public class PermissionService : IPermissionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMemoryCache _cache;

    // 🔹 ADD THIS
    private static readonly SemaphoreSlim _lock = new(1, 1);

    public PermissionService(IUnitOfWork unitOfWork, IMemoryCache cache)
    {
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    public async Task<List<UserFormPermissionModel>> GetUserPermissionsAsync(int userId)
    {
        var cacheKey = $"user-permissions-{userId}";

        // 🔹 1. Return from cache if exists
        if (_cache.TryGetValue(cacheKey, out List<UserFormPermissionModel>? cachedPermissions))
        {
            return cachedPermissions ?? new();
        }

        // 🔹 2. Prevent concurrent DB hits
        await _lock.WaitAsync();
        try
        {
            // Double-check inside lock
            if (_cache.TryGetValue(cacheKey, out cachedPermissions))
            {
                return cachedPermissions ?? new();
            }

            var permissions = await LoadPermissionsFromDbAsync(userId);

            // 🔹 3. Store in cache
            _cache.Set(cacheKey,permissions,new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
                SlidingExpiration = TimeSpan.FromMinutes(10)
            });

            return permissions;
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<List<UserFormPermissionModel>> LoadPermissionsFromDbAsync(int userId)
    {
        var userRoleRepo = _unitOfWork.GetRepository<UsersInRole>();
        var roleRepo = _unitOfWork.GetRepository<Roles>();
        var rolePermRepo = _unitOfWork.GetRepository<RoleFormPermission>();
        var formRepo = _unitOfWork.GetRepository<FormMaster>();

        var userRole = (await userRoleRepo.FindAsync(x =>
            x.UserId == userId && !x.IsDeleted))
            .FirstOrDefault();

        if (userRole == null)
            return new List<UserFormPermissionModel>();

        var role = await roleRepo.GetByIdAsync(userRole.RoleId);

        var permissions = new List<UserFormPermissionModel>();

        if (role == null)
            return permissions;

        var admin = "Admin";
        var factoryAdmin = "FactoryAdmin";
        // 🔹 Admin shortcut
        if (role.Name.ToLower() == admin.ToLower())
        {
            permissions.Add(new UserFormPermissionModel
            {
                Route = "*",
                CanView = true,
                CanCreate = true,
                CanEdit = true,
                CanDelete = true
            });

            return permissions;
        }

        if (role.Name.ToLower() == factoryAdmin.ToLower())
        {
            permissions.Add(new UserFormPermissionModel
            {
                Route = "*",
                CanView = true,
                CanCreate = true,
                CanEdit = true,
                CanDelete = true
            });
            return permissions;
        }

        var forms = await formRepo.FindAsync(x => !x.IsDeleted && x.IsActive);
        var rolePerms = await rolePermRepo.FindAsync(x =>
            x.RoleId == role.Id && !x.IsDeleted);

        foreach (var form in forms)
        {
            var rolePerm = rolePerms.FirstOrDefault(x => x.FormId == form.Id);

            if (rolePerm != null)
            {
                permissions.Add(new UserFormPermissionModel
                {
                    Route = form.Route,
                    CanView = rolePerm.CanView,
                    CanCreate = rolePerm.CanCreate,
                    CanEdit = rolePerm.CanEdit,
                    CanDelete = rolePerm.CanDelete
                });
            }
        }

        return permissions;
    }
    public void ClearUserPermissionCache(int userId)
    {
        var cacheKey = $"user-permissions-{userId}";
        _cache.Remove(cacheKey);
    }

}
