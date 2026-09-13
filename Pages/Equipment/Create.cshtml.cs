using EquipmentMonitor.Data;
using EquipmentMonitor.Models;
using EquipmentMonitor.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EquipmentMonitor.Pages.Equipment;

[Authorize]
public class CreateModel(AppDbContext db, ITenantProvider tenant) : PageModel
{
    [BindProperty]
    public Models.Equipment Equipment { get; set; } = new() { InstalledDate = DateTime.UtcNow.Date };

    public IActionResult OnGet() => Page();

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        Equipment.TenantId      = tenant.TenantId;
        Equipment.InstalledDate = DateTime.SpecifyKind(Equipment.InstalledDate, DateTimeKind.Utc);
        db.Equipments.Add(Equipment);
        await db.SaveChangesAsync();
        return RedirectToPage("/Index");
    }
}
