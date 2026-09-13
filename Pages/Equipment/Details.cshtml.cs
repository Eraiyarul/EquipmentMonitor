using EquipmentMonitor.Data;
using EquipmentMonitor.Models;
using EquipmentMonitor.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EquipmentMonitor.Pages.Equipment;

[Authorize]
public class DetailsModel(AppDbContext db, ITenantProvider tenant) : PageModel
{
    public Models.Equipment Equipment { get; private set; } = null!;
    public List<Reading> RecentReadings { get; private set; } = [];
    public List<Alert> RecentAlerts { get; private set; } = [];
    public List<Threshold> Thresholds { get; private set; } = [];

    // Date-range filter (default: last 24 hours)
    [BindProperty(SupportsGet = true)]
    public DateTime? From { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? To { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var eq = await db.Equipments.FindAsync(id);
        if (eq is null || eq.TenantId != tenant.TenantId) return NotFound();

        Equipment = eq;

        // Apply date-range defaults (last 24 h if not specified)
        var from = From.HasValue
            ? DateTime.SpecifyKind(From.Value, DateTimeKind.Utc)
            : DateTime.UtcNow.AddHours(-24);
        var to = To.HasValue
            ? DateTime.SpecifyKind(To.Value.AddDays(1).AddSeconds(-1), DateTimeKind.Utc)
            : DateTime.UtcNow;

        RecentReadings = await db.Readings
            .Where(r => r.EquipmentId == id
                     && r.Timestamp >= from
                     && r.Timestamp <= to)
            .OrderByDescending(r => r.Timestamp)
            .Take(200)
            .ToListAsync();

        RecentAlerts = await db.Alerts
            .Where(a => a.EquipmentId == id)
            .OrderByDescending(a => a.CreatedAt)
            .Take(10)
            .ToListAsync();

        Thresholds = await db.Thresholds
            .Where(t => t.EquipmentId == id)
            .ToListAsync();

        return Page();
    }

    public async Task<IActionResult> OnPostAcknowledgeAsync(int alertId, int equipmentId)
    {
        var eq = await db.Equipments.FindAsync(equipmentId);
        if (eq is null || eq.TenantId != tenant.TenantId) return Forbid();

        var alert = await db.Alerts.FindAsync(alertId);
        if (alert is not null && alert.Status == AlertStatus.Active)
        {
            alert.Status = AlertStatus.Acknowledged;
            await db.SaveChangesAsync();
        }
        return RedirectToPage(new { id = equipmentId });
    }

    public async Task<IActionResult> OnPostResolveAsync(int alertId, int equipmentId)
    {
        var eq = await db.Equipments.FindAsync(equipmentId);
        if (eq is null || eq.TenantId != tenant.TenantId) return Forbid();

        var alert = await db.Alerts.FindAsync(alertId);
        if (alert is not null && alert.Status != AlertStatus.Resolved)
        {
            alert.Status     = AlertStatus.Resolved;
            alert.ResolvedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
        return RedirectToPage(new { id = equipmentId });
    }
}
