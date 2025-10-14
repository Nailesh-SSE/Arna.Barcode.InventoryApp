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

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await ValidateAuth();
        }
    }

    protected virtual async Task ValidateAuth()
    {
        IsLoading = true;
        StateHasChanged();

        try
        {
            var isValid = await AuthValidationService.ValidateAndRefreshTokenAsync();

            if (!isValid)
            {
                IsAuthenticated = false;
                IsLoading = false;
                StateHasChanged();
                return;
            }

            var authState = await AuthenticationState.GetAuthenticationStateAsync();
            IsAuthenticated = authState.User.Identity?.IsAuthenticated ?? false;

            if (!IsAuthenticated)
            {
                Navigation.NavigateTo("/login", true);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Auth validation error: {ex.Message}");
            await AuthValidationService.ForceLogoutAsync();
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