using System.Security.Claims;

namespace EquipmentMonitor.Services;

public interface ITenantProvider
{
    int TenantId { get; }
    string TenantName { get; }
    string TenantCode { get; }
    string UserFullName { get; }
    string UserRole { get; }
}

public class HttpTenantProvider(IHttpContextAccessor accessor) : ITenantProvider
{
    private string Claim(string type) =>
        accessor.HttpContext?.User?.FindFirstValue(type) ?? string.Empty;

    public int TenantId
    {
        get
        {
            var v = Claim("TenantId");
            return int.TryParse(v, out var id) ? id : 0;
        }
    }

    public string TenantName  => Claim("TenantName");
    public string TenantCode  => Claim("TenantCode");
    public string UserFullName => Claim("FullName");
    public string UserRole    => Claim("UserRole");
}
