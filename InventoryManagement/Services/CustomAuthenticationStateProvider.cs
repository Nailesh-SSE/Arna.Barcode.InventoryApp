using InventoryManagement.Services.Interfaces;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace InventoryManagement.Core;

public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly IUserService _userService;
    private ClaimsPrincipal _anonymous = new ClaimsPrincipal(new ClaimsIdentity());

    public CustomAuthenticationStateProvider(IUserService userService)
    {
        _userService = userService;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        return await Task.FromResult(new AuthenticationState(_anonymous));
    }

    public async Task<bool> LoginAsync(string username, string password)
    {
        var isValid = await _userService.AuthenticateAsync(username, password);
        if (isValid)
        {
            var user = await _userService.GetUserByUsernameAsync(username);
            if (user != null)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.UserName),
                    new Claim(ClaimTypes.Email, user.EmailId),
                    new Claim("UserId", user.Id.ToString()),
                    new Claim("FullName", user.Name)
                };

                var identity = new ClaimsIdentity(claims, "apiauth");
                var userPrincipal = new ClaimsPrincipal(identity);

                NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(userPrincipal)));
                return true;
            }
        }
        return false;
    }

    public void Logout()
    {
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_anonymous)));
    }
}