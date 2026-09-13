using EquipmentMonitor.Data;
using EquipmentMonitor.Models;
using EquipmentMonitor.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EquipmentMonitor.Pages.Equipment;

[Authorize]
public class EditModel(AppDbContext db, ITenantProvider tenant) : PageModel
{
    [BindProperty]
    public Models.Equipment Equipment { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var eq = await db.Equipments.FindAsync(id);
        if (eq is null || eq.TenantId != tenant.TenantId) return NotFound();
        Equipment = eq;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var eq = await db.Equipments.FindAsync(Equipment.Id);
        if (eq is null || eq.TenantId != tenant.TenantId) return Forbid();

        eq.Name          = Equipment.Name;
        eq.Type          = Equipment.Type;
        eq.Location      = Equipment.Location;
        eq.Status        = Equipment.Status;
        eq.InstalledDate = DateTime.SpecifyKind(Equipment.InstalledDate, DateTimeKind.Utc);

        await db.SaveChangesAsync();
        return RedirectToPage("/Equipment/Details", new { id = eq.Id });
    }
}
