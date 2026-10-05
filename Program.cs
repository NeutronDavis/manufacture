using Manufacture.Api;
using Manufacture.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AddPageRoute("/Production/Recipes/Create", "Recipes/Create");
    options.Conventions.AddPageRoute("/Production/Recipes/Index", "Recipes");
    options.Conventions.AddPageRoute("/Production/Recipes/Details", "Recipes/Details/{id:int}");
    options.Conventions.AddPageRoute("/Production/Recipes/Edit", "Recipes/Edit/{id:int}");
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
builder.Services.AddSingleton<MockSalesService>();
builder.Services.AddSingleton<MockPayrollService>();
builder.Services.AddSingleton<MockLogisticsService>();
builder.Services.AddSingleton<MockDashboardService>();
builder.Services.AddSingleton<MockRbacService>();

// Production Recipe Builder & Cost Engine (doc/recipe-costing.md).
// Resolves live ingredient prices, so it must be registered AFTER the
// production service it depends on.
builder.Services.AddSingleton<RecipeCostingEngine>();

// Executive dashboard & reports engine (doc/dashboard-reports.md).
// Projects the product catalog from the recipe master and the fleet from the
// logistics service, so it must be registered AFTER both.
builder.Services.AddSingleton<MockAnalyticsService>();

var app = builder.Build();

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
