using InventoryManagement.Core.Data;
using InventoryManagement.Core.Entities;
using InventoryManagement.Infrastructure.Repositories;
using InventoryManagement.Services.Auth;
using InventoryManagement.Services.Filters;
using InventoryManagement.Services.Interfaces;
using InventoryManagement.Services.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Services;

public static class ServiceExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddRazorPages();
        services.AddHttpContextAccessor();

        services.AddDbContext<InventoryDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("ERP_Connection")));

        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Repository and Unit of Work
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Business Services
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ICompanyService, CompanyService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IInwardService, InwardService>();
        services.AddScoped<IOutwardService, OutwardService>();
        services.AddScoped<ISaleReturnService, SaleReturnService>();
        services.AddScoped<IColourService, ColourService>();

        // Register generators
        services.AddTransient<IInstockReportGenerator, InstockReportGenerator>();
        services.AddTransient<IOutstockReportGenerator, OutstockReportGenerator>();
        services.AddTransient<IReturnSaleReportGenerator, ReturnSaleReportGenerator>();

        // Register main service
        services.AddTransient<IReportService, ReportService>();

        services.AddAuthorizationCore();
        services.AddScoped<ProtectedSessionStorage>();
        services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();
        services.AddScoped<CustomAuthStateProvider>();

        return services;
    }

    public static WebApplication ConfigureMiddlewarePipeline(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseAntiforgery();

        return app;
    }
}
