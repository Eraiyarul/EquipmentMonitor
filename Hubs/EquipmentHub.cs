using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace EquipmentMonitor.Hubs;

[Authorize]
public class EquipmentHub : Hub
{
    // On connection, join the tenant-specific group so broadcasts are tenant-isolated.
    public override async Task OnConnectedAsync()
    {
        var tenantId = Context.User?.FindFirstValue("TenantId");
        if (!string.IsNullOrEmpty(tenantId))
            await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant-{tenantId}");

        await base.OnConnectedAsync();
    }
}
