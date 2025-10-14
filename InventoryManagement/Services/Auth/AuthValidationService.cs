// Services/Auth/AuthValidationService.cs
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
    private readonly AuthService _authService;
    private readonly NavigationManager _navigation;
    private readonly ILogger<AuthValidationService> _logger;

    public AuthValidationService(
        AuthenticationStateProvider authStateProvider,
        AuthService authService,
        NavigationManager navigation,
        ILogger<AuthValidationService> logger)
    {
        _authStateProvider = authStateProvider;
        _authService = authService;
        _navigation = navigation;
        _logger = logger;
    }

    public async Task<bool> IsTokenValidAsync()
    {
        try
        {
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            if (!user.Identity.IsAuthenticated)
            {
                _logger.LogInformation("User is not authenticated");
                return false;
            }

            var expClaim = user.FindFirst("exp")?.Value;
            if (!string.IsNullOrEmpty(expClaim) && long.TryParse(expClaim, out var exp))
            {
                var expiryTime = DateTimeOffset.FromUnixTimeSeconds(exp);
                if (expiryTime < DateTimeOffset.UtcNow)
                {
                    _logger.LogWarning("Token expired at {ExpiryTime}", expiryTime);
                    return false;
                }
            }

            var authTicketExpires = user.FindFirst("AuthTicketExpires")?.Value;
            if (!string.IsNullOrEmpty(authTicketExpires) && DateTime.TryParse(authTicketExpires, out var ticketExpiry))
            {
                if (ticketExpiry < DateTime.UtcNow)
                {
                    _logger.LogWarning("Auth ticket expired at {TicketExpiry}", ticketExpiry);
                    return false;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token");
            return false;
        }
    }

    public async Task<bool> ValidateAndRefreshTokenAsync()
    {
        var isValid = await IsTokenValidAsync();

        if (!isValid)
        {
            await ForceLogoutAsync();
            return false;
        }

        return true;
    }

    public async Task ForceLogoutAsync()
    {
        _logger.LogInformation("Forcing logout due to invalid token");

        _navigation.NavigateTo("/account/logout", true);

        await Task.CompletedTask;
    }
}