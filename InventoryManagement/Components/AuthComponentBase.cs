using InventoryManagement.Services.Auth;
using InventoryManagement.Services.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace InventoryManagement.Components.Auth;

public abstract class AuthComponentBase : ComponentBase, IDisposable
{
    [Inject] protected AuthenticationStateProvider AuthProvider { get; set; } = default!;
    [Inject] protected IPermissionService PermissionService { get; set; } = default!;
    [Inject] protected PermissionState PermissionState { get; set; } = default!;
    [Inject] protected NavigationManager NavigationManager { get; set; } = default!;

    protected ClaimsPrincipal User { get; private set; } = new(new ClaimsIdentity());
    protected int UserId => int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : 0;
    protected string CurrentUserId => UserId.ToString();
    protected bool IsAuthenticated => User.Identity?.IsAuthenticated ?? false;

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthProvider.GetAuthenticationStateAsync();
        User = authState.User;

        if (User.Identity?.IsAuthenticated == true && !PermissionState.IsLoaded)
        {
            PermissionState.SetPermissions(
                await PermissionService.GetUserPermissionsAsync(UserId)
            );
        }

        AuthProvider.AuthenticationStateChanged += OnAuthChanged;
    }

    private async void OnAuthChanged(Task<AuthenticationState> task)
    {
        var authState = await task;
        User = authState.User;

        if (!User.Identity?.IsAuthenticated ?? true)
            PermissionState.Clear();

        await InvokeAsync(StateHasChanged);
    }

    public bool CanView(string route)
    {
        var permission = PermissionState.Permissions.FirstOrDefault(p =>
            p.Route == "*" || p.Route.Equals(route, StringComparison.OrdinalIgnoreCase));
        return permission?.CanView ?? false;
    }

    public bool CanCreate(string route)
    {
        var permission = PermissionState.Permissions.FirstOrDefault(p =>
            p.Route == "*" || p.Route.Equals(route, StringComparison.OrdinalIgnoreCase));
        return permission?.CanCreate ?? false;
    }

    public bool CanEdit(string route)
    {
        var permission = PermissionState.Permissions.FirstOrDefault(p =>
            p.Route == "*" || p.Route.Equals(route, StringComparison.OrdinalIgnoreCase));
        return permission?.CanEdit ?? false;
    }

    public bool CanDelete(string route)
    {
        var permission = PermissionState.Permissions.FirstOrDefault(p =>
            p.Route == "*" || p.Route.Equals(route, StringComparison.OrdinalIgnoreCase));
        return permission?.CanDelete ?? false;
    }

    public bool IsAdmin()
    {
        var permission = PermissionState.Permissions.FirstOrDefault(p => p.Route == "*");
        return permission != null && permission.CanView && permission.CanCreate &&
               permission.CanEdit && permission.CanDelete;
    }

    public void Dispose()
        => AuthProvider.AuthenticationStateChanged -= OnAuthChanged;
}
