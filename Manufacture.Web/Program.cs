using Manufacture.Api;
using Manufacture.Library.Data;
using Manufacture.Library.Interfaces;
using Manufacture.Models.Entities;
using Manufacture.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Database-first data access. Entities and the DbContext were scaffolded from
// Manufacture.Database into Manufacture.Library/Data.
var manufactureConnectionString = builder.Configuration.GetConnectionString("ManufactureDb")
    ?? throw new InvalidOperationException("Connection string 'ManufactureDb' is not configured.");
builder.Services.AddDbContext<ManufactureDbContext>(options =>
    options.UseSqlServer(manufactureConnectionString));

// Database-backed user management and credential hashing.
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();

// Add services to the container.
builder.Services.AddRazorPages(options =>
{
    // Every Razor Page except Login/Logout/Error requires an authenticated session.
    options.Conventions.ConfigureFilter(new RequireSessionPageFilter());

    options.Conventions.AddPageRoute("/Production/Recipes/Create", "Recipes/Create");
    options.Conventions.AddPageRoute("/Production/Recipes/Index", "Recipes");
    options.Conventions.AddPageRoute("/Production/Recipes/Details", "Recipes/Details/{id:int}");
    options.Conventions.AddPageRoute("/Production/Recipes/Edit", "Recipes/Edit/{id:int}");

    // Fleet & Logistics Aliases (doc/plan.txt)
    options.Conventions.AddPageRoute("/Logistics/Rates/Index", "Fleet/Rates");
    options.Conventions.AddPageRoute("/Logistics/Rates/Create", "Fleet/Rates/Create");
    options.Conventions.AddPageRoute("/Logistics/Rates/Create", "Fleet/Rates/New");
    options.Conventions.AddPageRoute("/Logistics/Rates/Edit", "Fleet/Rates/Edit/{id:int}");
    options.Conventions.AddPageRoute("/Logistics/Charter/Index", "Fleet/Charter");
    options.Conventions.AddPageRoute("/Logistics/Charter/Create", "Fleet/Charter/New");
    options.Conventions.AddPageRoute("/Logistics/Charter/Details", "Fleet/Charter/Details/{id:int}");
    options.Conventions.AddPageRoute("/Logistics/WeeklyRunningCosts/Index", "Fleet/Weekly-Running-Costs");
    options.Conventions.AddPageRoute("/Logistics/WeeklyRunningCosts/Index", "Fleet/WeeklyRunningCosts");
    options.Conventions.AddPageRoute("/Logistics/Maintenance/Create", "Fleet/Maintenance/Create");
    options.Conventions.AddPageRoute("/Logistics/Maintenance/Create", "Fleet/Maintenance/New");
    options.Conventions.AddPageRoute("/Logistics/FuelLogs/Create", "Fleet/FuelLogs/Create");
    options.Conventions.AddPageRoute("/Logistics/FuelLogs/Create", "Fleet/FuelLogs/New");
    options.Conventions.AddPageRoute("/Logistics/Vehicles/Create", "Fleet/Vehicles/Create");
    options.Conventions.AddPageRoute("/Logistics/Vehicles/Create", "Fleet/Vehicles/New");
    options.Conventions.AddPageRoute("/Sales/PosAccounts/Index", "Sales/Supermarkets");
    options.Conventions.AddPageRoute("/Sales/PosAccounts/Create", "Sales/Supermarkets/Create");
    options.Conventions.AddPageRoute("/Sales/PosAccounts/Edit", "Sales/Supermarkets/Edit/{id:int?}");
    options.Conventions.AddPageRoute("/Sales/PosAccounts/Supply", "Sales/Supermarkets/Supply/{id:int?}");
    options.Conventions.AddPageRoute("/Sales/PosAccounts/Payment", "Sales/Supermarkets/Payment/{id:int?}");
});
builder.Services.AddHttpContextAccessor();

// Add Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Register Mock Services as Singletons for shared in-memory data
builder.Services.AddSingleton<IUnitConversionService, UnitConversionService>();
builder.Services.AddSingleton<MockUserService>();
builder.Services.AddSingleton<MockEmployeeService>();
builder.Services.AddSingleton<MockInventoryService>();
builder.Services.AddSingleton<MockProductionService>();
builder.Services.AddSingleton<MockLogisticsService>();
builder.Services.AddSingleton<MockSalesService>();
builder.Services.AddSingleton<MockPayrollService>();
builder.Services.AddSingleton<MockDashboardService>();

// Database-backed RBAC with a read cache for synchronous layout checks.
builder.Services.AddSingleton<RbacCache>();
builder.Services.AddScoped<IRbacService, RbacService>();

// Database-backed HR, payroll, and configuration services.
builder.Services.AddSingleton<SettingsCache>();
builder.Services.AddScoped<ISettingsService, SettingsService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IPayrollService, PayrollService>();
builder.Services.AddScoped<IFleetService, FleetService>();

// Production Recipe Builder & Cost Engine (doc/recipe-costing.md).
// Resolves live ingredient prices, so it must be registered AFTER the
// production service it depends on.
builder.Services.AddSingleton<RecipeCostingEngine>();

// Executive dashboard & reports engine (doc/dashboard-reports.md).
// Projects the product catalog from the recipe master and the fleet from the
// logistics service, so it must be registered AFTER both.
builder.Services.AddSingleton<MockAnalyticsService>();

var app = builder.Build();

// Warm the RBAC cache so the first layout render has the database matrix.
using (var startupScope = app.Services.CreateScope())
{
    try
    {
        var rbacService = startupScope.ServiceProvider.GetRequiredService<IRbacService>();
        await rbacService.RefreshCacheAsync();
    }
    catch (Exception ex)
    {
        var startupLogger = startupScope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        startupLogger.LogError(ex, "RBAC cache warm-up failed. Falling back to Super Admin-only access.");
    }

    try
    {
        var settingsService = startupScope.ServiceProvider.GetRequiredService<ISettingsService>();
        await settingsService.RefreshCacheAsync();
    }
    catch (Exception ex)
    {
        var startupLogger = startupScope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        startupLogger.LogError(ex, "Settings cache warm-up failed. Default settings will be used.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// Support reverse proxies (like Render.com, Cloudflare, NGINX)
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
});

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthorization();

app.MapRazorPages();

// Read-only analytics endpoints backing the dashboard and the reports engine.
app.MapAnalyticsApi();

// Dual-mode recipe costing endpoints.
app.MapRecipeApi();

app.Run();
