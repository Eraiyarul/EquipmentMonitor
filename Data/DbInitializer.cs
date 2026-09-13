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
        var acme = new Tenant { Name = "Acme Industries",    Code = "ACME", CreatedAt = DateTime.UtcNow };
        var tech = new Tenant { Name = "TechCorp Solutions", Code = "TECH", CreatedAt = DateTime.UtcNow };
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

        // ── Equipment — Acme Industries ──────────────────────────────────
        var acmeStations = new[]
        {
            new Equipment { Name="Pump Station A",   Type="Hydraulic Pump",    Location="Floor 1 - North Wing", Status=EquipmentStatus.Active,           InstalledDate=Utc(2024,1,10),  TenantId=acme.Id },
            new Equipment { Name="Compressor Unit B", Type="Air Compressor",   Location="Floor 2 - East Bay",   Status=EquipmentStatus.Active,           InstalledDate=Utc(2024,3,22),  TenantId=acme.Id },
            new Equipment { Name="Conveyor Belt C",  Type="Belt Conveyor",     Location="Warehouse - Section C", Status=EquipmentStatus.UnderMaintenance, InstalledDate=Utc(2024,6,5),   TenantId=acme.Id },
        };
        db.Equipments.AddRange(acmeStations);

        // ── Equipment — TechCorp Solutions ──────────────────────────────
        var techStations = new[]
        {
            new Equipment { Name="Server Rack Alpha", Type="Rack Server",  Location="Data Centre - Rack A12", Status=EquipmentStatus.Active, InstalledDate=Utc(2024,5,1),  TenantId=tech.Id },
            new Equipment { Name="CNC Machine Delta", Type="CNC Milling",  Location="Production Floor - Bay 3", Status=EquipmentStatus.Active, InstalledDate=Utc(2024,8,15), TenantId=tech.Id },
        };
        db.Equipments.AddRange(techStations);

        await db.SaveChangesAsync();

        // ── Thresholds ───────────────────────────────────────────────────
        var thresholds = new List<Threshold>();

        // Pump Station A — industrial pump ranges
        thresholds.Add(new() { EquipmentId=acmeStations[0].Id, MetricType="Temperature", MinValue=60,  MaxValue=90  });
        thresholds.Add(new() { EquipmentId=acmeStations[0].Id, MetricType="Humidity",    MinValue=30,  MaxValue=70  });
        thresholds.Add(new() { EquipmentId=acmeStations[0].Id, MetricType="Pressure",    MinValue=0.8, MaxValue=1.2 });

        // Compressor Unit B — higher pressure tolerances
        thresholds.Add(new() { EquipmentId=acmeStations[1].Id, MetricType="Temperature", MinValue=50,  MaxValue=80  });
        thresholds.Add(new() { EquipmentId=acmeStations[1].Id, MetricType="Humidity",    MinValue=20,  MaxValue=65  });
        thresholds.Add(new() { EquipmentId=acmeStations[1].Id, MetricType="Pressure",    MinValue=1.0, MaxValue=2.5 });

        // Conveyor Belt C — lower thermal range
        thresholds.Add(new() { EquipmentId=acmeStations[2].Id, MetricType="Temperature", MinValue=20,  MaxValue=50  });
        thresholds.Add(new() { EquipmentId=acmeStations[2].Id, MetricType="Humidity",    MinValue=25,  MaxValue=75  });
        thresholds.Add(new() { EquipmentId=acmeStations[2].Id, MetricType="Pressure",    MinValue=0.5, MaxValue=1.0 });

        // Server Rack Alpha — strict data centre ranges
        thresholds.Add(new() { EquipmentId=techStations[0].Id, MetricType="Temperature", MinValue=18,  MaxValue=28  });
        thresholds.Add(new() { EquipmentId=techStations[0].Id, MetricType="Humidity",    MinValue=40,  MaxValue=60  });
        thresholds.Add(new() { EquipmentId=techStations[0].Id, MetricType="Pressure",    MinValue=0.9, MaxValue=1.1 });

        // CNC Machine Delta
        thresholds.Add(new() { EquipmentId=techStations[1].Id, MetricType="Temperature", MinValue=20,  MaxValue=45  });
        thresholds.Add(new() { EquipmentId=techStations[1].Id, MetricType="Humidity",    MinValue=30,  MaxValue=65  });
        thresholds.Add(new() { EquipmentId=techStations[1].Id, MetricType="Pressure",    MinValue=0.6, MaxValue=1.8 });

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
