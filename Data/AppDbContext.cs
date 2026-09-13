using EquipmentMonitor.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EquipmentMonitor.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<Equipment> Equipments { get; set; }
    public DbSet<Reading> Readings { get; set; }
    public DbSet<Alert> Alerts { get; set; }
    public DbSet<Threshold> Thresholds { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Reading>()
            .HasIndex(r => new { r.EquipmentId, r.Timestamp });

        modelBuilder.Entity<Alert>()
            .HasIndex(a => new { a.EquipmentId, a.Status });

        modelBuilder.Entity<Equipment>()
            .HasIndex(e => e.TenantId);

        modelBuilder.Entity<Equipment>()
            .Property(e => e.Status)
            .HasConversion<string>();

        modelBuilder.Entity<Alert>()
            .Property(a => a.Status)
            .HasConversion<string>();

        // ApplicationUser -> Tenant FK
        modelBuilder.Entity<ApplicationUser>()
            .HasOne(u => u.Tenant)
            .WithMany()
            .HasForeignKey(u => u.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        // Equipment -> Tenant FK
        modelBuilder.Entity<Equipment>()
            .HasOne(e => e.Tenant)
            .WithMany(t => t.Equipments)
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
