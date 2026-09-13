namespace EquipmentMonitor.Models;

public class Equipment
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public EquipmentStatus Status { get; set; } = EquipmentStatus.Active;
    public DateTime InstalledDate { get; set; }

    // Tenant ownership
    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public ICollection<Reading> Readings { get; set; } = [];
    public ICollection<Alert> Alerts { get; set; } = [];
    public ICollection<Threshold> Thresholds { get; set; } = [];
}

public enum EquipmentStatus
{
    Active,
    Idle,
    Faulty,
    UnderMaintenance
}
