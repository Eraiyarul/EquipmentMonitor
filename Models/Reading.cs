namespace EquipmentMonitor.Models;

public class Reading
{
    public int Id { get; set; }
    public int EquipmentId { get; set; }
    public Equipment Equipment { get; set; } = null!;

    public string MetricType { get; set; } = string.Empty;   // Temperature, Humidity, Pressure
    public double Value { get; set; }
    public string Unit { get; set; } = string.Empty;          // °C, %, hPa
    public DateTime Timestamp { get; set; }
}
