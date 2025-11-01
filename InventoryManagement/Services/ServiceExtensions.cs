using InventoryManagement.Core.Data;
using InventoryManagement.Infrastructure.Repositories;
using InventoryManagement.Services.Auth;
using InventoryManagement.Services.Interfaces;
using InventoryManagement.Services.Services;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Services;

public static class ServiceExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddRazorPages();

        services.AddDbContext<InventoryDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("ERP_Connection")));

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
        services.AddScoped<IColourService, ColourService>();

        // Simplified Authentication only
        services.AddScoped<AuthService>();

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
        app.UseAntiforgery();

        return app;
    }
}
