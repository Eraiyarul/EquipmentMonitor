using EquipmentMonitor.Models;
using Microsoft.AspNetCore.Identity;

namespace EquipmentMonitor.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(AppDbContext db, IServiceProvider services)
    {
        if (db.Tenants.Any()) return;   // already seeded

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        // ── Tenants ─────────────────────────────────────────────────────
        var acme = new Tenant { Name = "Acme Industries",     Code = "ACME", CreatedAt = DateTime.UtcNow };
        var tech = new Tenant { Name = "TechCorp Solutions",  Code = "TECH", CreatedAt = DateTime.UtcNow };
        db.Tenants.AddRange(acme, tech);
        await db.SaveChangesAsync();

        // ── Users ────────────────────────────────────────────────────────
        await CreateUser(userManager, new ApplicationUser
        {
            UserName = "admin", Email = "admin@acme.io",
            FullName = "Admin User", UserRole = "Admin", TenantId = acme.Id, EmailConfirmed = true
        }, "Admin@123");

        await CreateUser(userManager, new ApplicationUser
        {
            UserName = "operator", Email = "operator@acme.io",
            FullName = "Plant Operator", UserRole = "Operator", TenantId = acme.Id, EmailConfirmed = true
        }, "Operator@123");

        await CreateUser(userManager, new ApplicationUser
        {
            UserName = "techop", Email = "ops@techcorp.io",
            FullName = "Tech Operator", UserRole = "Operator", TenantId = tech.Id, EmailConfirmed = true
        }, "Techop@123");

        // ── Equipment & Thresholds — Acme ───────────────────────────────
        var acmeStations = new[]
        {
            new Equipment { Name="Station Alpha", Type="Weather Sensor", Location="Rooftop",  Status=EquipmentStatus.Active, InstalledDate=Utc(2024,1,10),  TenantId=acme.Id },
            new Equipment { Name="Station Beta",  Type="Weather Sensor", Location="Basement", Status=EquipmentStatus.Active, InstalledDate=Utc(2024,3,22),  TenantId=acme.Id },
            new Equipment { Name="Station Gamma", Type="Weather Sensor", Location="Garden",   Status=EquipmentStatus.Idle,   InstalledDate=Utc(2024,6,5),   TenantId=acme.Id },
        };
        db.Equipments.AddRange(acmeStations);

        // ── Equipment & Thresholds — TechCorp ───────────────────────────
        var techStations = new[]
        {
            new Equipment { Name="Sensor X1", Type="Env Monitor", Location="Server Room", Status=EquipmentStatus.Active, InstalledDate=Utc(2024,5,1),  TenantId=tech.Id },
            new Equipment { Name="Sensor X2", Type="Env Monitor", Location="Lab Floor",   Status=EquipmentStatus.Active, InstalledDate=Utc(2024,8,15), TenantId=tech.Id },
        };
        db.Equipments.AddRange(techStations);

        await db.SaveChangesAsync();

        // ── Thresholds ───────────────────────────────────────────────────
        var thresholds = new List<Threshold>();

        foreach (var s in acmeStations)
        {
            thresholds.Add(new() { EquipmentId=s.Id, MetricType="Temperature", MinValue=10, MaxValue=40 });
            thresholds.Add(new() { EquipmentId=s.Id, MetricType="Humidity",    MinValue=20, MaxValue=80 });
            thresholds.Add(new() { EquipmentId=s.Id, MetricType="Pressure",    MinValue=950, MaxValue=1050 });
        }
        foreach (var s in techStations)
        {
            thresholds.Add(new() { EquipmentId=s.Id, MetricType="Temperature", MinValue=18, MaxValue=28 });
            thresholds.Add(new() { EquipmentId=s.Id, MetricType="Humidity",    MinValue=30, MaxValue=60 });
            thresholds.Add(new() { EquipmentId=s.Id, MetricType="Pressure",    MinValue=980, MaxValue=1030 });
        }

        db.Thresholds.AddRange(thresholds);
        await db.SaveChangesAsync();
    }

    private static async Task CreateUser(UserManager<ApplicationUser> um, ApplicationUser user, string password)
    {
        if (await um.FindByNameAsync(user.UserName!) is null)
            await um.CreateAsync(user, password);
    }

    private static DateTime Utc(int y, int m, int d) =>
        new(y, m, d, 0, 0, 0, DateTimeKind.Utc);
}
