using InventoryManagement.Services.Models;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;


namespace InventoryManagement.Services.Auth;

public class ClientPermissionService
{
    private const string PermissionKey = "UserPermissions";

    private readonly ProtectedLocalStorage _storage;

    public ClientPermissionService(ProtectedLocalStorage storage)
    {
        _storage = storage;
    }

    public async Task SaveAsync(List<UserFormPermissionModel> permissions)
    {
        await _storage.SetAsync(PermissionKey, permissions);
    }

    public async Task<List<UserFormPermissionModel>?> LoadAsync()
    {
        var result = await _storage.GetAsync<List<UserFormPermissionModel>>(PermissionKey);
        return result.Success ? result.Value : null;
    }

    public async Task ClearAsync()
    {
        await _storage.DeleteAsync(PermissionKey);
    }
}
