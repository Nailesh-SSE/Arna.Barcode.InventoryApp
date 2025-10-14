using InventoryManagement.Services;
var builder = WebApplication.CreateBuilder(args);

// --- Service Registration ---
// 1. Add infrastructure services (Database, Storage, etc.)
builder.Services.AddInfrastructureServices(builder.Configuration);

// 2. Add application-specific services (Business Logic)
builder.Services.AddApplicationServices();

// 3. Add Authentication & Authorization services
builder.Services.AddAuthenticationAndAuthorization();

// 4. Add UI and Web specific services
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddBlazorBootstrap();
var app = builder.Build();

// --- Middleware & Endpoint Configuration ---
app.ConfigureMiddlewarePipeline();
app.MapApplicationEndpoints();

app.Run();
