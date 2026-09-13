using Microsoft.AspNetCore.Identity;

namespace EquipmentMonitor.Models;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public string UserRole { get; set; } = "Operator";   // Admin | Operator | Viewer
    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
}
