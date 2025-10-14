
using InventoryManagement.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace InventoryManagement.Services.Auth;

public class AuthService
{
    private readonly IUserService _userService;

    public AuthService(IUserService userService)
    {
        _userService = userService;
    }

    public async Task<IResult> LoginAsync(HttpContext httpContext, string username, string password, string? returnUrl)
    {
        try
        {
            var isValid = await _userService.AuthenticateAsync(username, password);
            if (!isValid)
                return RedirectWithError("/login", "Invalid username or password", returnUrl);

            var user = await _userService.GetUserByUsernameAsync(username);
            if (user == null)
                return RedirectWithError("/login", "User not found", returnUrl);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.UserName),
                new(ClaimTypes.Email, user.EmailId ?? ""),
                new("FullName", user.Name ?? ""),
                new("UserId", user.Id.ToString())
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8),
                RedirectUri = returnUrl ?? "/"
            };

            await httpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                claimsPrincipal,
                authProperties);

            return Results.Redirect(string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Login error: {ex.Message}");
            return RedirectWithError("/login", "An error occurred during login", returnUrl);
        }
    }

    public async Task<IResult> LogoutAsync(HttpContext httpContext)
    {
        try
        {
            await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            if (httpContext.Session != null)
                httpContext.Session.Clear();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Logout error: {ex.Message}");
        }

        return Results.Redirect("/login");
    }

    private static IResult RedirectWithError(string path, string error, string? returnUrl)
    {
        var query = $"?error={Uri.EscapeDataString(error)}";
        if (!string.IsNullOrEmpty(returnUrl))
            query += $"&returnUrl={Uri.EscapeDataString(returnUrl)}";

        return Results.Redirect(path + query);
    }
}
