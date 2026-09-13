using EquipmentMonitor.Data;
using EquipmentMonitor.Hubs;
using EquipmentMonitor.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace EquipmentMonitor.Services;

public class AlertEvaluationService(AppDbContext db, IHubContext<EquipmentHub> hub)
{
    public async Task EvaluateAsync(Reading reading, int tenantId)
    {
        var threshold = await db.Thresholds
            .FirstOrDefaultAsync(t => t.EquipmentId == reading.EquipmentId
                                   && t.MetricType  == reading.MetricType);

        if (threshold is null) return;

        bool breached = reading.Value < threshold.MinValue || reading.Value > threshold.MaxValue;
        if (!breached) return;

        string direction = reading.Value < threshold.MinValue ? "below minimum" : "above maximum";
        var alert = new Alert
        {
            EquipmentId = reading.EquipmentId,
            ReadingId   = reading.Id,
            Message     = $"{reading.MetricType} is {direction}: {reading.Value}{reading.Unit} " +
                          $"(safe range {threshold.MinValue}–{threshold.MaxValue}{reading.Unit})",
            Status      = AlertStatus.Active,
            CreatedAt   = DateTime.UtcNow
        };

        db.Alerts.Add(alert);
        await db.SaveChangesAsync();

        var name = await db.Equipments
            .Where(e => e.Id == reading.EquipmentId)
            .Select(e => e.Name)
            .FirstOrDefaultAsync() ?? "Unknown";

        // Broadcast only to the owning tenant's SignalR group
        await hub.Clients.Group($"tenant-{tenantId}").SendAsync("NewAlert", new
        {
            alert.Id,
            EquipmentId   = reading.EquipmentId,
            EquipmentName = name,
            alert.Message,
            Status        = alert.Status.ToString(),
            CreatedAt     = alert.CreatedAt.ToString("HH:mm:ss")
        });
    }
}
