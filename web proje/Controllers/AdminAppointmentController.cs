// Controllers/AdminAppointmentController.cs

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FitnessCenterProject.Data;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;

// Sadece Admin rolüne sahip kullanıcılar bu Controller'a erişebilir.
[Authorize(Roles = "Admin")]
public class AdminAppointmentController : Controller
{
    private readonly ApplicationDbContext _context;

    public AdminAppointmentController(ApplicationDbContext context)
    {
        _context = context;
    }

    // ----------------------- TÜM RANDEVULARI LİSTELEME -----------------------
    // GET: AdminAppointment
    public async Task<IActionResult> Index()
    {
        // Tüm randevuları (kullanıcıya bakılmaksızın) çeker
        var appointments = await _context.Appointments
            .Include(a => a.Trainer)
            .Include(a => a.Service)
            .Include(a => a.User) // Randevuyu alan kullanıcıyı da dahil et
            .OrderByDescending(a => a.StartTime) // En yeni randevuyu üste getir
            .ToListAsync();

        return View(appointments);
    }

    // ----------------------- RANDEVU ONAYLAMA (Action) -----------------------
    // POST: AdminAppointment/Confirm/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(int id)
    {
        var appointment = await _context.Appointments.FindAsync(id);

        if (appointment == null)
        {
            return NotFound();
        }

        // Randevunun durumunu Onaylandı (True) olarak değiştir
        appointment.IsConfirmed = true;

        _context.Update(appointment);
        await _context.SaveChangesAsync();

        // Başarılı onaydan sonra listeye geri dön
        return RedirectToAction(nameof(Index));
    }
}