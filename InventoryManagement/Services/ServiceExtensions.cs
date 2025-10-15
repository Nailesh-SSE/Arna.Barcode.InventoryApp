using InventoryManagement.Components;
using InventoryManagement.Core.Data;
using InventoryManagement.Infrastructure.Repositories;
using InventoryManagement.Services.Auth;
using InventoryManagement.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Services;

public static  class ServiceExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
       services.AddDbContext<InventoryDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("ERP_Connection")));

        services.AddScoped<ProtectedSessionStorage>();
        services.AddHttpContextAccessor();

        return services;
    }
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ICompanyService, CompanyService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IInwardService, InwardService>();
        services.AddScoped<IOutwardService, OutwardService>();
        services.AddScoped<ISaleReturnService, SaleReturnService>();

        services.AddScoped<AuthService>();
        services.AddScoped<IAuthValidationService, AuthValidationService>();

        return services;
    }
    public static IServiceCollection AddAuthenticationAndAuthorization(this IServiceCollection services)
    {
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = "InventoryManagement.Auth";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.LoginPath = "/login";
                options.LogoutPath = "/account/logout";
                options.AccessDeniedPath = "/access-denied";
                options.ExpireTimeSpan = TimeSpan.FromSeconds(120);
                options.SlidingExpiration = true;
                options.Events = new CookieAuthenticationEvents
                {
                    OnValidatePrincipal = async context =>
                    {
                        var expires = context.Properties?.ExpiresUtc;
                        if (expires.HasValue && expires.Value < DateTimeOffset.UtcNow)
                        {
                            Console.WriteLine("Cookie expired, signing out...");
                            context.RejectPrincipal();
                            await context.HttpContext.SignOutAsync();
                        }
                    },
                    OnRedirectToLogin = context =>
                    {
                        context.Response.StatusCode = 401;
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorizationCore(options =>
        {
            options.AddPolicy("RequireAuthenticatedUser", policy => policy.RequireAuthenticatedUser());
        });

        services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
        services.AddCascadingAuthenticationState();

        return services;
    }

    public static WebApplication ConfigureMiddlewarePipeline(this WebApplication app)
    {
        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseAntiforgery();

        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }

    public static WebApplication MapApplicationEndpoints(this WebApplication app)
    {
        // Map Razor Components
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        // Map custom API or auth endpoints
        app.MapAuthEndpoints();

        return app;
    }
}
