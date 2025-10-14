using Microsoft.AspNetCore.Mvc;

namespace InventoryManagement.Services.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/account").WithTags("Account");

        group.MapPost("/login", async (
            HttpContext context,
            [FromForm] string username,
            [FromForm] string password,
            [FromForm] string? returnUrl,
            AuthService authService) =>
        {
            return await authService.LoginAsync(context, username, password, returnUrl);
        }).AllowAnonymous();

        group.MapPost("/logout", async (HttpContext context, AuthService authService) =>
        {
            return await authService.LogoutAsync(context);
        }).RequireAuthorization();

        group.MapGet("/logout", async (HttpContext context, AuthService authService) =>
        {
            await authService.LogoutAsync(context);
            context.Response.Redirect("/login");
        });

        return app;
    }
}
