using EquipmentMonitor.Data;
using EquipmentMonitor.Models;
using EquipmentMonitor.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EquipmentMonitor.Pages.Alerts;

[Authorize]
public class IndexModel(AppDbContext db, ITenantProvider tenant) : PageModel
{
    public List<Alert> ActiveAlerts { get; private set; } = [];
    public List<Alert> ResolvedAlerts { get; private set; } = [];

    public async Task OnGetAsync()
    {
        var tid = tenant.TenantId;

        ActiveAlerts = await db.Alerts
            .Include(a => a.Equipment)
            .Where(a => a.Equipment.TenantId == tid && a.Status != AlertStatus.Resolved)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        ResolvedAlerts = await db.Alerts
            .Include(a => a.Equipment)
            .Where(a => a.Equipment.TenantId == tid && a.Status == AlertStatus.Resolved)
            .OrderByDescending(a => a.ResolvedAt)
            .Take(20)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostAcknowledgeAsync(int id)
    {
        var alert = await db.Alerts.Include(a => a.Equipment).FirstOrDefaultAsync(a => a.Id == id);
        if (alert is not null && alert.Equipment.TenantId == tenant.TenantId
            && alert.Status == AlertStatus.Active)
        {
            alert.Status = AlertStatus.Acknowledged;
            await db.SaveChangesAsync();
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostResolveAsync(int id)
    {
        var alert = await db.Alerts.Include(a => a.Equipment).FirstOrDefaultAsync(a => a.Id == id);
        if (alert is not null && alert.Equipment.TenantId == tenant.TenantId
            && alert.Status != AlertStatus.Resolved)
        {
            alert.Status     = AlertStatus.Resolved;
            alert.ResolvedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
        return RedirectToPage();
    }
}
