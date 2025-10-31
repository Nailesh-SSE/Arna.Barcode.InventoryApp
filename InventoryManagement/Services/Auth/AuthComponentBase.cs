using InventoryManagement.Services.Auth;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace InventoryManagement.Components;

public class AuthComponentBase : ComponentBase, IDisposable
{
    [Inject] protected IAuthValidationService AuthValidationService { get; set; }
    [Inject] protected NavigationManager Navigation { get; set; }
    [Inject] protected AuthenticationStateProvider AuthenticationState { get; set; }

    protected bool IsAuthenticated { get; private set; }
    protected bool IsLoading { get; private set; } = true;

    protected override async Task OnInitializedAsync()
    {
        await ValidateAuth();
    }

    protected virtual async Task ValidateAuth()
    {
        // Don't validate on login page - this prevents the loop
        if (Navigation.Uri.Contains("/login", StringComparison.OrdinalIgnoreCase))
        {
            IsLoading = false;
            IsAuthenticated = false;
            return;
        }

        IsLoading = true;
        StateHasChanged();

        try
        {
            var authState = await AuthenticationState.GetAuthenticationStateAsync();
            var user = authState.User;

            IsAuthenticated = user.Identity?.IsAuthenticated ?? false;

            if (!IsAuthenticated)
            {
                Console.WriteLine("User not authenticated, redirecting to login");
                // Use a simple return URL to avoid loops
                var currentUri = new Uri(Navigation.Uri);
                var returnUrl = currentUri.PathAndQuery;

                // Only redirect if we're not already going to login
                if (!returnUrl.Contains("/login"))
                {
                    Navigation.NavigateTo($"/login?returnUrl={Uri.EscapeDataString(returnUrl)}", true);
                }
                return;
            }

            Console.WriteLine("User is authenticated successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Auth validation error: {ex.Message}");
            // Don't redirect with the current URL to avoid loops
            Navigation.NavigateTo("/login", true);
        }
        finally
        {
            IsLoading = false;
            StateHasChanged();
        }
    }

    public virtual void Dispose()
    {
    }
}