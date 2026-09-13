namespace EquipmentMonitor.Models;

public class Threshold
{
    public int Id { get; set; }
    public int EquipmentId { get; set; }
    public Equipment Equipment { get; set; } = null!;

    public string MetricType { get; set; } = string.Empty;
    public double MinValue { get; set; }
    public double MaxValue { get; set; }
}
