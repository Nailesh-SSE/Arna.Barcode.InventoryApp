using InventoryManagement.Core.Entities;
using InventoryManagement.Infrastructure.Repositories;
using InventoryManagement.Services.Interfaces;
using InventoryManagement.Services.Mapper;
using InventoryManagement.Services.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace InventoryManagement.Services.Services;

public class PermissionService : IPermissionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMemoryCache _cache;
    private readonly ILogger<PermissionService> _logger;
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

    public PermissionService(IUnitOfWork unitOfWork, IMemoryCache cache, ILogger<PermissionService> logger)
    {
        _unitOfWork = unitOfWork;
        _cache = cache;
        _logger = logger;
    }

    public async Task<List<UserFormPermissionModel>> GetUserPermissionsAsync(int userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = GetPermissionCacheKey(userId);

            if (TryGetFromCache(cacheKey, out List<UserFormPermissionModel>? cachedPermissions))
                return cachedPermissions;

            // Ensure only one thread loads permissions at a time
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                // Double-check cache after acquiring lock
                if (TryGetFromCache(cacheKey, out cachedPermissions))
                    return cachedPermissions;

                cancellationToken.ThrowIfCancellationRequested();

                var permissions = await LoadPermissionsFromDbAsync(userId, cancellationToken);
                SetCache(cacheKey, permissions);
                return permissions;
            }
            finally
            {
                _semaphore.Release();
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Permission loading cancelled for user {UserId}", userId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading permissions for user {UserId}", userId);
            throw;
        }
    }

    public async Task<List<FormMasterModel>> GetPermittedFormsAsync(int userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = GetFormsCacheKey(userId);

            if (TryGetFromCache(cacheKey, out List<FormMasterModel>? cachedForms))
                return cachedForms;

            cancellationToken.ThrowIfCancellationRequested();

            var permissions = await GetUserPermissionsAsync(userId, cancellationToken);
            List<FormMaster> forms = permissions.Any(p => p.Route == "*")
                ? await GetAllActiveFormsAsync(cancellationToken)
                : await GetPermittedFormsAsync(permissions, cancellationToken);

            var result = forms.ToModelList();
            SetCache(cacheKey, result);
            return result;
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Form loading cancelled for user {UserId}", userId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading permitted forms for user {UserId}", userId);
            throw;
        }
    }

    private async Task<List<FormMaster>> GetAllActiveFormsAsync(CancellationToken cancellationToken)
    {
        try
        {
            return (await _unitOfWork
                .GetRepository<FormMaster>()
                .FindAsync(f => !f.IsDeleted && f.IsActive, cancellationToken))
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading all active forms");
            throw;
        }
    }

    private async Task<List<FormMaster>> GetPermittedFormsAsync(List<UserFormPermissionModel> permissions, CancellationToken cancellationToken)
    {
        try
        {
            var allowedFormIds = permissions
                .Where(p => p.CanView)
                .Select(p => p.FormId)
                .ToHashSet();

            return (await _unitOfWork
                .GetRepository<FormMaster>()
                .FindAsync(f =>
                    !f.IsDeleted &&
                    f.IsActive &&
                    allowedFormIds.Contains(f.Id), cancellationToken))
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading permitted forms");
            throw;
        }
    }

    private async Task<List<UserFormPermissionModel>> LoadPermissionsFromDbAsync(int userId, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var userRoleRepo = _unitOfWork.GetRepository<UsersInRole>();
            var roleRepo = _unitOfWork.GetRepository<Roles>();
            var rolePermRepo = _unitOfWork.GetRepository<RoleFormPermission>();
            var formRepo = _unitOfWork.GetRepository<FormMaster>();

            var userRole = (await userRoleRepo.FindAsync(x =>
                x.UserId == userId && !x.IsDeleted, cancellationToken)).FirstOrDefault();

            if (userRole is null)
                return [];

            var role = await roleRepo.GetByIdAsync(userRole.RoleId, cancellationToken);
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
            
            var forms = (await formRepo.FindAsync(x => !x.IsDeleted && x.IsActive, cancellationToken)).ToList();
            var rolePerms = (await rolePermRepo.FindAsync(x =>
                x.RoleId == role.Id && !x.IsDeleted, cancellationToken)).ToList();

            var permissions = forms
                .Select(form =>
                {
                    var rolePerm = rolePerms.FirstOrDefault(x => x.FormId == form.Id);
                    return rolePerm is not null
                        ? new UserFormPermissionModel
                        {
                            Route = form.Route,
                            FormId = form.Id,
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
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading permissions from database for user {UserId}", userId);
            throw;
        }
    }

    public void ClearUserPermissionCache(int userId)
    {
        try
        {
            _cache.Remove(GetPermissionCacheKey(userId));
            _cache.Remove(GetFormsCacheKey(userId));
            _logger.LogDebug("Cleared permission cache for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing permission cache for user {UserId}", userId);
        }
    }

    private static bool IsAdminRole(string roleName) => roleName.Equals("Admin", StringComparison.OrdinalIgnoreCase);
    private static bool IsFactoryAdminRole(string roleName) => roleName.Equals("FactoryAdmin", StringComparison.OrdinalIgnoreCase);

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
        try
        {
            _cache.Set(cacheKey, value, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(8),
                SlidingExpiration = TimeSpan.FromHours(8)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache for key {CacheKey}", cacheKey);
        }
    }
}
