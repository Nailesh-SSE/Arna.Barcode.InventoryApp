using Microsoft.AspNetCore.Components;
using InventoryManagement.Services.Auth;

namespace InventoryManagement.Components;

public class AuthComponentBase : ComponentBase
{
    [Inject] protected AuthService AuthService { get; set; } = default!;
    [Inject] protected NavigationManager Navigation { get; set; } = default!;

    protected bool IsAuthenticated { get; private set; }
    protected bool IsLoading { get; private set; } = true;

    protected override async Task OnInitializedAsync()
    {
        await ValidateAuth();
    }

    protected virtual Task ValidateAuth()
    {
        // Skip login page
        if (Navigation.Uri.Contains("/login", StringComparison.OrdinalIgnoreCase))
        {
            IsLoading = false;
            IsAuthenticated = false;
            return Task.CompletedTask;
        }

        IsAuthenticated = AuthService.IsAuthenticated();

        if (!IsAuthenticated)
        {
            Navigation.NavigateTo("/login", true);
        }

        IsLoading = false;
        StateHasChanged();
        return Task.CompletedTask;
    }
}
