using InventoryManagement.Components;
using InventoryManagement.Services;

var builder = WebApplication.CreateBuilder(args);

// --- Service Registration ---
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddApplicationServices();

// Add Razor Components (for .NET 8+ Blazor Server)
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddBlazorBootstrap();

var app = builder.Build();

// --- Middleware & Endpoint Configuration ---
app.ConfigureMiddlewarePipeline();

// This replaces the old _Host file – entry point for your app
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
