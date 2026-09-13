using EquipmentMonitor.Data;
using EquipmentMonitor.Hubs;
using EquipmentMonitor.Models;
using EquipmentMonitor.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ── EF Core + PostgreSQL ──────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── ASP.NET Core Identity ─────────────────────────────────────────────────────
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(o =>
{
    o.Password.RequireDigit           = true;
    o.Password.RequiredLength         = 6;
    o.Password.RequireUppercase       = true;
    o.Password.RequireNonAlphanumeric = false;
    o.SignIn.RequireConfirmedAccount  = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders()
.AddClaimsPrincipalFactory<AppClaimsPrincipalFactory>();

// ── Cookie Auth ───────────────────────────────────────────────────────────────
builder.Services.ConfigureApplicationCookie(o =>
{
    o.LoginPath       = "/Account/Login";
    o.LogoutPath      = "/Account/Logout";
    o.AccessDeniedPath = "/Account/Login";
    o.ExpireTimeSpan  = TimeSpan.FromHours(8);
    o.SlidingExpiration = true;
    o.Cookie.Name     = "IoTMonitor.Auth";
});

// ── Razor Pages + SignalR ─────────────────────────────────────────────────────
builder.Services.AddRazorPages(o =>
{
    o.Conventions.AuthorizeFolder("/");            // protect all pages
    o.Conventions.AllowAnonymousToPage("/Account/Login");
    o.Conventions.AllowAnonymousToPage("/Account/Logout");
}).AddRazorRuntimeCompilation();

builder.Services.AddSignalR();

// ── App Services ──────────────────────────────────────────────────────────────
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantProvider, HttpTenantProvider>();
builder.Services.AddScoped<AlertEvaluationService>();
builder.Services.AddScoped<ReadingIngestionService>();
builder.Services.AddHostedService<MqttBackgroundService>();

// ── App ───────────────────────────────────────────────────────────────────────
var app = builder.Build();

// Drop + recreate DB on every start in Development (gives clean slate for demo)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (app.Environment.IsDevelopment())
        db.Database.EnsureDeleted();
    db.Database.Migrate();
    await DbInitializer.SeedAsync(db, scope.ServiceProvider);
}

if (!app.Environment.IsDevelopment())
    app.UseExceptionHandler("/Error");

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapHub<EquipmentHub>("/hubs/equipment");

app.Run();
