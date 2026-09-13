using EquipmentMonitor.Data;
using EquipmentMonitor.Models;
using EquipmentMonitor.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EquipmentMonitor.Pages.Equipment;

[Authorize]
public class DeleteModel(AppDbContext db, ITenantProvider tenant) : PageModel
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
        var eq = await db.Equipments.FindAsync(Equipment.Id);
        if (eq is null || eq.TenantId != tenant.TenantId) return Forbid();

        db.Equipments.Remove(eq);
        await db.SaveChangesAsync();
        return RedirectToPage("/Index");
    }
}
