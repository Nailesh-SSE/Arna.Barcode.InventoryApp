using InventoryManagement.Components;
using InventoryManagement.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddApplicationServices();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddHubOptions(options =>
    {
        options.MaximumReceiveMessageSize = 10 * 1024 * 1024; // 10MB
    });

builder.Services.AddBlazorBootstrap();

var app = builder.Build();

app.ConfigureMiddlewarePipeline();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
