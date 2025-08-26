using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using DataPilot.Web.Data;
using DataPilot.Web.Services;
using DataPilot.Web.Providers.Db;
using DataPilot.Web.Providers.Llm;

var builder = WebApplication.CreateBuilder(args);

// Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.File("logs/app-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();
builder.Host.UseSerilog();

// EF Core MetaDb
builder.Services.AddDbContext<DataPilotMetaDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("MetaDb")));

// Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<DataPilotMetaDbContext>()
    .AddDefaultTokenProviders();

// MVC
builder.Services.AddControllersWithViews();

// HttpClient
builder.Services.AddHttpClient("llm");

// Core services
builder.Services.AddSingleton<CryptoService>();
builder.Services.AddSingleton<SqlSafetyGuard>();
builder.Services.AddScoped<QueryService>();
builder.Services.AddScoped<SchemaService>();
builder.Services.AddSingleton<LlmClientFactory>();

// DB connector factory and connectors
builder.Services.AddSingleton<IDbConnectorFactory, DbConnectorFactory>();
builder.Services.AddScoped<SqlServerConnector>();

var app = builder.Build();

// Dev bypass
if (app.Environment.IsDevelopment() && (builder.Configuration["Auth:DevBypass"] == "true"))
{
    // Sign-in middleware can be added later; for now just log
    Log.Information("DevBypass is enabled – auto-auth can be wired in controllers.");
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
