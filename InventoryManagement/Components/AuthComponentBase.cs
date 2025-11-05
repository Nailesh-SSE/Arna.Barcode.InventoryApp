using InventoryManagement.Services.Auth;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace InventoryManagement.Components.Auth
{
    public class AuthComponentBase : ComponentBase, IDisposable
    {
        [Inject] protected AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
        [Inject] protected NavigationManager NavigationManager { get; set; } = default!;

        protected bool IsAuthenticated { get; set; }
        protected bool IsLoading { get; set; } = true;
        protected ClaimsPrincipal CurrentUser { get; set; } = default!;
        protected string CurrentUserId => CurrentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        protected string CurrentUserName => CurrentUser.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty;
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

                // Navigate to login page using client-side navigation
                NavigationManager.NavigateTo("/login", false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Logout error: {ex.Message}");
            }
        }

        protected void NavigateToLogin(string returnUrl = "")
        {
            var loginUrl = "/login";
            if (!string.IsNullOrEmpty(returnUrl))
            {
                loginUrl += $"?returnUrl={Uri.EscapeDataString(returnUrl)}";
            }

            // Use client-side navigation to preserve session state
            NavigationManager.NavigateTo(loginUrl, false);
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
}
