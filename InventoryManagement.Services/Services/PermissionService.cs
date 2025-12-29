using InventoryManagement.Core.Entities;
using InventoryManagement.Infrastructure.Repositories;
using InventoryManagement.Services.Interfaces;
using InventoryManagement.Services.Mapper;
using InventoryManagement.Services.Models;
using Microsoft.Extensions.Caching.Memory;

namespace InventoryManagement.Services.Services;

public class PermissionService : IPermissionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMemoryCache _cache;
    private static readonly SemaphoreSlim _lock = new(1, 1);

    public PermissionService(IUnitOfWork unitOfWork, IMemoryCache cache)
    {
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    public async Task<List<UserFormPermissionModel>> GetUserPermissionsAsync(int userId)
    {
        var cacheKey = GetPermissionCacheKey(userId);

        if (TryGetFromCache(cacheKey, out List<UserFormPermissionModel>? cachedPermissions))
            return cachedPermissions;

        await _lock.WaitAsync();
        try
        {
            if (TryGetFromCache(cacheKey, out cachedPermissions))
                return cachedPermissions;

            var permissions = await LoadPermissionsFromDbAsync(userId);
            SetCache(cacheKey, permissions);

            return permissions;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<List<FormMasterModel>> GetPermittedFormsAsync(int userId)
    {
        var cacheKey = GetFormsCacheKey(userId);

        if (TryGetFromCache(cacheKey, out List<FormMasterModel>? cachedForms))
            return cachedForms;

        await _lock.WaitAsync();
        try
        {
            if (TryGetFromCache(cacheKey, out cachedForms))
                return cachedForms;

            var permissions = await GetUserPermissionsAsync(userId);

            List<FormMaster> forms = permissions.Any(p => p.Route == "*")
                ? await GetAllActiveFormsAsync()
                : await GetPermittedFormsAsync(permissions);

            var result = forms.ToModelList();
            SetCache(cacheKey, result);

            return result;
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<List<FormMaster>> GetAllActiveFormsAsync()
    {
        return (await _unitOfWork
            .GetRepository<FormMaster>()
            .FindAsync(f => !f.IsDeleted && f.IsActive))
            .ToList();
    }

    private async Task<List<FormMaster>> GetPermittedFormsAsync(List<UserFormPermissionModel> permissions)
    {
        var allowedRoutes = permissions
            .Where(p => p.CanView)
            .Select(p => p.Route)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return (await _unitOfWork
            .GetRepository<FormMaster>()
            .FindAsync(f =>
                !f.IsDeleted &&
                f.IsActive &&
                allowedRoutes.Contains(f.Route)))
            .ToList();
    }

    private async Task<List<UserFormPermissionModel>> LoadPermissionsFromDbAsync(int userId)
    {
        var userRoleRepo = _unitOfWork.GetRepository<UsersInRole>();
        var roleRepo = _unitOfWork.GetRepository<Roles>();
        var rolePermRepo = _unitOfWork.GetRepository<RoleFormPermission>();
        var formRepo = _unitOfWork.GetRepository<FormMaster>();

        var userRole = (await userRoleRepo.FindAsync(x =>
            x.UserId == userId && !x.IsDeleted)).FirstOrDefault();

        if (userRole is null)
            return [];

        var role = await roleRepo.GetByIdAsync(userRole.RoleId);

        if (role is null)
            return [];

        if (IsAdminRole(role.Name))
        {
            return [new UserFormPermissionModel
            {
                Route = "*",
                CanView = true,
                CanCreate = true,
                CanEdit = true,
                CanDelete = true
            }];
        }

        var forms = await formRepo.FindAsync(x => !x.IsDeleted && x.IsActive);
        var rolePerms = await rolePermRepo.FindAsync(x =>
            x.RoleId == role.Id && !x.IsDeleted);

        var permissions = forms
            .Select(form =>
            {
                var rolePerm = rolePerms.FirstOrDefault(x => x.FormId == form.Id);
                return rolePerm is not null
                    ? new UserFormPermissionModel
                    {
                        Route = form.Route,
                        CanView = rolePerm.CanView,
                        CanCreate = rolePerm.CanCreate,
                        CanEdit = rolePerm.CanEdit,
                        CanDelete = rolePerm.CanDelete
                    }
                    : null;
            })
            .Where(p => p is not null)
            .ToList()!;

        return permissions;
    }

    public void ClearUserPermissionCache(int userId)
    {
        _cache.Remove(GetPermissionCacheKey(userId));
        _cache.Remove(GetFormsCacheKey(userId));
    }

    private static bool IsAdminRole(string roleName) =>
        roleName.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
        roleName.Equals("FactoryAdmin", StringComparison.OrdinalIgnoreCase);

    private static string GetPermissionCacheKey(int userId) => $"user-permissions-{userId}";
    private static string GetFormsCacheKey(int userId) => $"user-forms-{userId}";

    private bool TryGetFromCache<T>(string cacheKey, out T? value)
    {
        if (_cache.TryGetValue(cacheKey, out T? cachedValue) && cachedValue is not null)
        {
            value = cachedValue;
            return true;
        }
        value = default;
        return false;
    }

    private void SetCache<T>(string cacheKey, T value)
    {
        _cache.Set(cacheKey, value, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(120),
            SlidingExpiration = TimeSpan.FromMinutes(120)
        });
    }
}