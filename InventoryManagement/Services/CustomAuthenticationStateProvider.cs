using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace InventoryManagement;

public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CustomAuthenticationStateProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var user = _httpContextAccessor.HttpContext?.User;

        if (user == null || user.Identity?.IsAuthenticated != true)
        {
            // User is not authenticated
            var anonymousIdentity = new ClaimsIdentity();
            var anonymousPrincipal = new ClaimsPrincipal(anonymousIdentity);
            return Task.FromResult(new AuthenticationState(anonymousPrincipal));
        }

        // User is authenticated, return their claims principal
        return Task.FromResult(new AuthenticationState(user));
    }

}