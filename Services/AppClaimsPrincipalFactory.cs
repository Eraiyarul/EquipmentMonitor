using System.Security.Claims;
using EquipmentMonitor.Data;
using EquipmentMonitor.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace EquipmentMonitor.Services;

public class AppClaimsPrincipalFactory(
    UserManager<ApplicationUser> userManager,
    IOptions<IdentityOptions> options,
    AppDbContext db)
    : UserClaimsPrincipalFactory<ApplicationUser>(userManager, options)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        var tenant   = await db.Tenants.FindAsync(user.TenantId);

        identity.AddClaim(new Claim("TenantId",   user.TenantId.ToString()));
        identity.AddClaim(new Claim("TenantName", tenant?.Name ?? string.Empty));
        identity.AddClaim(new Claim("TenantCode", tenant?.Code ?? string.Empty));
        identity.AddClaim(new Claim("FullName",   user.FullName));
        identity.AddClaim(new Claim("UserRole",   user.UserRole));

        return identity;
    }
}
