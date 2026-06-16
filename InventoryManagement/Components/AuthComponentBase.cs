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
    [Inject] protected ILogger<AuthComponentBase> Logger { get; set; } = default!;

    protected ClaimsPrincipal User { get; private set; } = new(new ClaimsIdentity());
    protected int UserId => int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : 0;
    protected string CurrentUserId => UserId.ToString();
    protected int CurrentUserRoleId => int.TryParse(User.FindFirst("UserRoleId")?.Value, out var roleId) ? roleId : 0;
    protected int CurrentUserRoleLevel => int.TryParse(User.FindFirst("UserRoleLevel")?.Value, out var roleLevel) ? roleLevel : int.MaxValue;
    protected bool IsAuthenticated => User.Identity?.IsAuthenticated ?? false;

    private CancellationTokenSource _cts = new CancellationTokenSource();

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var authState = await AuthProvider.GetAuthenticationStateAsync();
            User = authState.User;

            if (IsAuthenticated && (PermissionState.Permissions.Count == 0 || !PermissionState.IsLoaded))
            {
                var permissions = await PermissionService.GetUserPermissionsAsync(UserId, _cts.Token);
                PermissionState.SetPermissions(permissions);

                await PermissionService.GetPermittedFormsAsync(UserId, _cts.Token);
            }

            AuthProvider.AuthenticationStateChanged += OnAuthChanged;
        }
        catch (OperationCanceledException)
        {
            Logger.LogDebug("Component initialization was cancelled");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error initializing auth component");
        }
    }

    private void OnAuthChanged(Task<AuthenticationState> task)
    {
        _ = HandleAuthChangedAsync(task);
    }

    private async Task HandleAuthChangedAsync(Task<AuthenticationState> task)
    {
        try
        {
            _cts.Token.ThrowIfCancellationRequested();

            var authState = await task;
            User = authState.User;

            if (!User.Identity?.IsAuthenticated ?? true)
            {
                PermissionState.Clear();
            }

            await InvokeAsync(StateHasChanged);
        }
        catch (OperationCanceledException)
        {
            // Expected when component is disposed
            Logger.LogDebug("Auth change handler was cancelled");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error handling auth state change");
        }
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

    public bool IsFactoryAdmin()
    {
        return CurrentUserRoleLevel > 0 && CurrentUserRoleLevel <= 5;
    }

    public void Dispose()
    {
        try
        {
            _cts?.Cancel();
            _cts?.Dispose();
            AuthProvider.AuthenticationStateChanged -= OnAuthChanged;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error disposing AuthComponentBase");
        }
    }
}
