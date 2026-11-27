using Manufacture.Api;
using Manufacture.Library.Data;
using Manufacture.Library.Interfaces;
using Manufacture.Models.Entities;
using Manufacture.Services;
using Manufacture.Services.Reports;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Database-first data access. Entities and the DbContext were scaffolded from
// Manufacture.Database into Manufacture.Library/Data.
var manufactureConnectionString = builder.Configuration.GetConnectionString("ManufactureDb")
    ?? throw new InvalidOperationException("Connection string 'ManufactureDb' is not configured.");

// Safe startup diagnostic: shows which connection options the running container
// actually received (never logs the user or password).
var dbOptions = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(manufactureConnectionString);

// Temporary escape hatch for hosts that cannot complete TLS with an older SQL
// Server. Set DB_ALLOW_UNENCRYPTED=true to force Encrypt=Optional regardless of
// what the connection string contains. Remove once SQL Server TLS is fixed.
var allowUnencrypted = string.Equals(
    builder.Configuration["DB_ALLOW_UNENCRYPTED"],
    "true",
    StringComparison.OrdinalIgnoreCase);
if (allowUnencrypted)
{
    dbOptions.Encrypt = Microsoft.Data.SqlClient.SqlConnectionEncryptOption.Optional;
    manufactureConnectionString = dbOptions.ConnectionString;
}

Console.WriteLine(
    $"[DB] Server={dbOptions.DataSource}; Database={dbOptions.InitialCatalog}; " +
    $"Encrypt={dbOptions.Encrypt}; TrustServerCertificate={dbOptions.TrustServerCertificate}; " +
    $"IntegratedSecurity={dbOptions.IntegratedSecurity}; MARS={dbOptions.MultipleActiveResultSets}; " +
    $"ForcedUnencrypted={allowUnencrypted}");

// One-shot startup probe: logs the exact database error (if any) before the
// first request, so a failing login does not require reproducing it from the UI.
try
{
    var probeOptions = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(manufactureConnectionString)
    {
        ConnectTimeout = 10
    };

    await using var probe = new Microsoft.Data.SqlClient.SqlConnection(probeOptions.ConnectionString);
    await probe.OpenAsync();
    await using var probeCommand = probe.CreateCommand();
    probeCommand.CommandText =
        "SELECT @@VERSION, encrypt_option FROM sys.dm_exec_connections WHERE session_id = @@SPID";
    await using var probeReader = await probeCommand.ExecuteReaderAsync();
    if (await probeReader.ReadAsync())
    {
        var versionLine = probeReader.IsDBNull(0)
            ? "unknown"
            : probeReader.GetString(0).Split('\n')[0].Trim();
        var encrypted = probeReader.IsDBNull(1) ? "unknown" : probeReader.GetString(1);
        Console.WriteLine($"[DB] Probe OK: {versionLine}; EncryptOption={encrypted}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"[DB] Probe FAILED: {ex.GetType().Name}: {ex.Message}");
    Console.WriteLine(ex);
}

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
    options.Conventions.AddPageRoute("/Logistics/Rates/Index", "Fleet/Quotes");
    options.Conventions.AddPageRoute("/Logistics/Rates/Create", "Fleet/Quotes/New");
    options.Conventions.AddPageRoute("/Logistics/Rates/Edit", "Fleet/Quotes/Edit/{id:int}");
    options.Conventions.AddPageRoute("/Logistics/Charter/Index", "Fleet/Charter");
    options.Conventions.AddPageRoute("/Logistics/Charter/Create", "Fleet/Charter/New");
    options.Conventions.AddPageRoute("/Logistics/Charter/Details", "Fleet/Charter/Details/{id:int}");
    options.Conventions.AddPageRoute("/Logistics/Dispatches/Index", "Fleet/Dispatches");
    options.Conventions.AddPageRoute("/Logistics/Dispatches/Create", "Fleet/Dispatches/New");
    options.Conventions.AddPageRoute("/Logistics/Dispatches/Details", "Fleet/Dispatches/Details/{id:int}");
    options.Conventions.AddPageRoute("/Logistics/WeeklyRunningCosts/Index", "Fleet/Weekly-Running-Costs");
    options.Conventions.AddPageRoute("/Logistics/WeeklyRunningCosts/Index", "Fleet/WeeklyRunningCosts");
    options.Conventions.AddPageRoute("/Logistics/Maintenance/Index", "Fleet/Maintenance");
    options.Conventions.AddPageRoute("/Logistics/Maintenance/Create", "Fleet/Maintenance/Create");
    options.Conventions.AddPageRoute("/Logistics/Maintenance/Create", "Fleet/Maintenance/New");
    options.Conventions.AddPageRoute("/Logistics/Maintenance/Details", "Fleet/Maintenance/Details/{id:int}");
    options.Conventions.AddPageRoute("/Logistics/FuelLogs/Create", "Fleet/FuelLogs/Create");
    options.Conventions.AddPageRoute("/Logistics/FuelLogs/Create", "Fleet/FuelLogs/New");
    options.Conventions.AddPageRoute("/Logistics/Vehicles/Create", "Fleet/Vehicles/Create");
    options.Conventions.AddPageRoute("/Logistics/Vehicles/Create", "Fleet/Vehicles/New");
    options.Conventions.AddPageRoute("/Logistics/Vehicles/Details", "Fleet/Vehicles/Details/{id:int}");
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
builder.Services.AddScoped<ICharterQuoteService, CharterQuoteService>();
builder.Services.AddScoped<IFleetDispatchService, FleetDispatchService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IMeasurementDensityService, MeasurementDensityService>();
builder.Services.AddScoped<ISalesService, SalesService>();
builder.Services.AddScoped<IProductRepositoryService, ProductRepositoryService>();
builder.Services.AddScoped<ISalesReconciliationService, SalesReconciliationService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<ProductStockLedger>();
builder.Services.AddScoped<IProductionService, ProductionService>();
builder.Services.AddScoped<ISuperAdminDashboardService, SuperAdminDashboardService>();
builder.Services.AddScoped<ReportAuthorizationService>();
builder.Services.AddScoped<ReportQueryService>();
builder.Services.AddScoped<IOrganizationService, OrganizationService>();
builder.Services.AddScoped<IImageUploadService, ImageUploadService>();

// Production Recipe Builder & Cost Engine (doc/recipe-costing.md).
// Resolves live ingredient prices, so it must be registered AFTER the
// production service it depends on.
builder.Services.AddScoped<RecipeCostingEngine>(sp =>
    new RecipeCostingEngine(sp.GetRequiredService<IProductionService>()));

// Executive dashboard & reports engine (doc/dashboard-reports.md).
// Projects the product catalog from the recipe master and the fleet from the
// logistics service, so it must be registered AFTER both.
builder.Services.AddSingleton<MockAnalyticsService>();

var app = builder.Build();

// Warm the RBAC cache so the first layout render has the database matrix.
using (var startupScope = app.Services.CreateScope())
{
    var startupLogger = startupScope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        var rbacService = startupScope.ServiceProvider.GetRequiredService<IRbacService>();
        try
        {
            var createdPermissions = await rbacService.InitializeMissingRolePermissionsAsync("System");
            if (createdPermissions > 0)
            {
                app.Logger.LogInformation(
                    "Initialised {Count} RBAC permission rows for roles that had none.",
                    createdPermissions);
            }
        }
        catch (Exception ex)
        {
            startupLogger.LogError(ex, "RBAC role permission backfill failed.");
        }

        await rbacService.RefreshCacheAsync();
    }
    catch (Exception ex)
    {
        startupLogger.LogError(ex, "RBAC cache warm-up failed. Falling back to Super Admin-only access.");
    }

    try
    {
        var settingsService = startupScope.ServiceProvider.GetRequiredService<ISettingsService>();
        await settingsService.RefreshCacheAsync();
    }
    catch (Exception ex)
    {
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
