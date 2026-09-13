namespace EquipmentMonitor.Models;

public class Alert
{
    public int Id { get; set; }
    public int EquipmentId { get; set; }
    public Equipment Equipment { get; set; } = null!;

    public int ReadingId { get; set; }
    public Reading Reading { get; set; } = null!;

    public string Message { get; set; } = string.Empty;
    public AlertStatus Status { get; set; } = AlertStatus.Active;
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
}

public enum AlertStatus
{
    Active,
    Acknowledged,
    Resolved
}
