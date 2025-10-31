using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
namespace InventoryManagement.Services.Auth;

public interface IAuthValidationService
{
    Task<bool> IsTokenValidAsync();
    Task<bool> ValidateAndRefreshTokenAsync();
    Task ForceLogoutAsync();
}
public class AuthValidationService : IAuthValidationService
{
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly NavigationManager _navigation;
    private readonly ILogger<AuthValidationService> _logger;

    public AuthValidationService(
        AuthenticationStateProvider authStateProvider,
        NavigationManager navigation,
        ILogger<AuthValidationService> logger)
    {
        _authStateProvider = authStateProvider;
        _navigation = navigation;
        _logger = logger;
    }

    public async Task<bool> ValidateAndRefreshTokenAsync()
    {
        try
        {
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            var isAuthenticated = authState.User?.Identity?.IsAuthenticated ?? false;

            if (!isAuthenticated)
            {
                _logger.LogWarning("User is not authenticated, redirecting to login");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating authentication state");
            return false;
        }
    }

    public Task<bool> IsTokenValidAsync() => ValidateAndRefreshTokenAsync();

    public Task ForceLogoutAsync()
    {
        _navigation.NavigateTo("/account/logout", true);
        return Task.CompletedTask;
    }
}
