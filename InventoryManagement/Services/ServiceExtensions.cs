using InventoryManagement.Core.Data;
using InventoryManagement.Infrastructure.Repositories;
using InventoryManagement.Services.Auth;
using InventoryManagement.Services.Interfaces;
using InventoryManagement.Services.Reports.Services;
using InventoryManagement.Services.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.EntityFrameworkCore;
using BlazorDownloadFile;

namespace InventoryManagement.Services;

public static class ServiceExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddRazorPages();
        services.AddHttpContextAccessor();

        services.AddDbContext<InventoryDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("ERP_Connection")));
        services.AddBlazorDownloadFile();
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
        services.AddScoped<IPlatformService, PlatformService>();

        // Register generators
        services.AddTransient<IInwardReportService, InwardReportGenerator>();
        services.AddTransient<IOutwardReportService, OutwardReportGenerator>();
        services.AddTransient<ISaleReturnReportService, SaleReturnReportGenerator>();

        services.AddAuthorizationCore();
        services.AddScoped<ProtectedLocalStorage>();
        services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();
        services.AddScoped<CustomAuthStateProvider>();
        services.AddScoped<OutwardPdfService>();

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
