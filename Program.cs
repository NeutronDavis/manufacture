using Manufacture.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
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
builder.Services.AddSingleton<MockUserService>();
builder.Services.AddSingleton<MockEmployeeService>();
builder.Services.AddSingleton<MockProductionService>();
builder.Services.AddSingleton<MockSalesService>();
builder.Services.AddSingleton<MockInventoryService>();
builder.Services.AddSingleton<MockPayrollService>();
builder.Services.AddSingleton<MockLogisticsService>();
builder.Services.AddSingleton<MockDashboardService>();

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

app.Run();
