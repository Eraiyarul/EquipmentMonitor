using EquipmentMonitor.Data;
using EquipmentMonitor.Models;
using EquipmentMonitor.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EquipmentMonitor.Pages;

[Authorize]
public class IndexModel(AppDbContext db, ITenantProvider tenant) : PageModel
{
    public List<EquipmentSummary> Equipments { get; private set; } = [];
    public int TotalReadingsToday { get; private set; }
    public int ActiveAlertCount { get; private set; }

    public async Task OnGetAsync()
    {
        var tid   = tenant.TenantId;
        var today = DateTime.UtcNow.Date;

        TotalReadingsToday = await db.Readings
            .CountAsync(r => r.Equipment.TenantId == tid && r.Timestamp >= today);

        ActiveAlertCount = await db.Alerts
            .CountAsync(a => a.Equipment.TenantId == tid && a.Status == AlertStatus.Active);

        Equipments = await db.Equipments
            .Where(e => e.TenantId == tid)
            .Select(e => new EquipmentSummary
            {
                Id            = e.Id,
                Name          = e.Name,
                Type          = e.Type,
                Location      = e.Location,
                Status        = e.Status,
                InstalledDate = e.InstalledDate,
                ActiveAlerts  = e.Alerts.Count(a => a.Status == AlertStatus.Active),
                LatestReadings = e.Readings
                    .OrderByDescending(r => r.Timestamp)
                    .Take(3)
                    .Select(r => new ReadingSummary
                    {
                        MetricType = r.MetricType,
                        Value      = r.Value,
                        Unit       = r.Unit,
                        Timestamp  = r.Timestamp
                    })
                    .ToList()
            })
            .ToListAsync();
    }

    public record EquipmentSummary
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Type { get; init; } = string.Empty;
        public string Location { get; init; } = string.Empty;
        public EquipmentStatus Status { get; init; }
        public DateTime InstalledDate { get; init; }
        public int ActiveAlerts { get; init; }
        public List<ReadingSummary> LatestReadings { get; init; } = [];
    }

    public record ReadingSummary
    {
        public string MetricType { get; init; } = string.Empty;
        public double Value { get; init; }
        public string Unit { get; init; } = string.Empty;
        public DateTime Timestamp { get; init; }
    }
}
