using InventoryManagement.Services.Auth;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace InventoryManagement.Components.Auth;

public class AuthComponentBase : ComponentBase, IDisposable
{
    [Inject] protected AuthenticationStateProvider AuthenticationStateProvider { get; set; }

    protected bool IsAuthenticated { get; set; }
    protected bool IsLoading { get; set; } = true;
    protected ClaimsPrincipal CurrentUser { get; set; }
    protected string CurrentUserId => CurrentUser.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
    protected string CurrentUserName => CurrentUser.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? string.Empty;
    protected string CurrentUserFullName => CurrentUser.FindFirst("FullName")?.Value ?? string.Empty;

    protected override async Task OnInitializedAsync()
    {
        await CheckAuthenticationState();

        // Subscribe to authentication state changes
        if (AuthenticationStateProvider != null)
        {
            AuthenticationStateProvider.AuthenticationStateChanged += OnAuthenticationStateChanged;
        }
    }

    private async void OnAuthenticationStateChanged(Task<AuthenticationState> task)
    {
        await CheckAuthenticationState();
        await InvokeAsync(StateHasChanged);
    }

    private async Task CheckAuthenticationState()
    {
        try
        {
            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            CurrentUser = authState.User;
            IsAuthenticated = authState.User.Identity?.IsAuthenticated ?? false;
        }
        catch (Exception ex)
        {
            // Log error if needed
            Console.WriteLine($"Error checking authentication state: {ex.Message}");
            CurrentUser = new ClaimsPrincipal(new ClaimsIdentity());
            IsAuthenticated = false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    protected async Task LogoutAsync()
    {
        try
        {
            var authProvider = AuthenticationStateProvider as CustomAuthStateProvider;
            if (authProvider != null)
            {
                await authProvider.MarkUserAsLoggedOut();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Logout error: {ex.Message}");
        }
    }

    public void Dispose()
    {
        try
        {
            if (AuthenticationStateProvider != null)
            {
                AuthenticationStateProvider.AuthenticationStateChanged -= OnAuthenticationStateChanged;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Disposal error: {ex.Message}");
        }
    }
}
