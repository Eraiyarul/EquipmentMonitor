using System.Text.Json;
using EquipmentMonitor.Data;
using EquipmentMonitor.Hubs;
using EquipmentMonitor.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace EquipmentMonitor.Services;

public class ReadingIngestionService(
    AppDbContext db,
    AlertEvaluationService alertService,
    IHubContext<EquipmentHub> hub)
{
    private static readonly JsonSerializerOptions JsonOpts =
        new() { PropertyNameCaseInsensitive = true };

    public async Task IngestAsync(string json)
    {
        var dto = JsonSerializer.Deserialize<ReadingDto>(json, JsonOpts);
        if (dto is null) return;

        var equipment = await db.Equipments
            .Include(e => e.Tenant)
            .FirstOrDefaultAsync(e => e.Id == dto.EquipmentId);

        if (equipment is null || equipment.Status == EquipmentStatus.Faulty) return;

        var reading = new Reading
        {
            EquipmentId = dto.EquipmentId,
            MetricType  = dto.MetricType,
            Value       = Math.Round(dto.Value, 1),
            Unit        = dto.Unit,
            Timestamp   = dto.Timestamp == default ? DateTime.UtcNow : dto.Timestamp
        };

        db.Readings.Add(reading);
        await db.SaveChangesAsync();

        var tenantId = equipment.TenantId;

        // Evaluate thresholds
        await alertService.EvaluateAsync(reading, tenantId);

        // Broadcast to the owning tenant's SignalR group only
        await hub.Clients.Group($"tenant-{tenantId}").SendAsync("NewReading", new
        {
            reading.Id,
            reading.EquipmentId,
            EquipmentName = equipment.Name,
            reading.MetricType,
            reading.Value,
            reading.Unit,
            Timestamp = reading.Timestamp.ToString("HH:mm:ss")
        });
    }

    private sealed record ReadingDto(
        int EquipmentId,
        string MetricType,
        double Value,
        string Unit,
        DateTime Timestamp);
}
