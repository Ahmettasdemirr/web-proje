// Controllers/AdminAppointmentController.cs

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FitnessCenterProject.Data;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using FitnessCenterProject.Models; // Appointment modelini kullanmak için

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
        // Tüm randevuları (kullanıcı, eğitmen, hizmet) çeker
        var appointments = await _context.Appointments
            .Include(a => a.Trainer)
            .Include(a => a.Service)
            .Include(a => a.User) // Randevuyu alan kullanıcıyı da dahil et
            .OrderByDescending(a => a.StartTime)
            .ToListAsync();

        return View(appointments);
    }

    // ----------------------- RANDEVU ONAYLAMA/REDDETME (Action) -----------------------
    // POST: AdminAppointment/Confirm/5
    // isConfirmed parametresi ile hem onaylama (true) hem reddetme (false) sağlanır.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(int id, bool isConfirmed)
    {
        var appointment = await _context.Appointments.FindAsync(id);

        if (appointment == null)
        {
            return NotFound();
        }

        appointment.IsConfirmed = isConfirmed;

        try
        {
            _context.Update(appointment);
            await _context.SaveChangesAsync();

            string status = isConfirmed ? "onaylandı" : "reddedildi";
            TempData["SuccessMessage"] = $"Randevu #{id} başarıyla {status}.";
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!_context.Appointments.Any(e => e.AppointmentId == id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        // Başarılı işlemden sonra listeye geri dön
        return RedirectToAction(nameof(Index));
    }

    // ----------------------- RANDEVU DETAYLARI -----------------------
    // GET: AdminAppointment/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var appointment = await _context.Appointments
            .Include(a => a.Trainer)
            .Include(a => a.Service)
            .Include(a => a.User)
            .FirstOrDefaultAsync(m => m.AppointmentId == id);

        if (appointment == null) return NotFound();

        return View(appointment);
    }

    // ----------------------- RANDEVU SİLME -----------------------
    // GET: AdminAppointment/Delete/5 (Onay Formu)
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var appointment = await _context.Appointments
            .Include(a => a.Trainer)
            .Include(a => a.Service)
            .Include(a => a.User)
            .FirstOrDefaultAsync(m => m.AppointmentId == id);

        if (appointment == null) return NotFound();

        return View(appointment);
    }

    // POST: AdminAppointment/Delete/5 (Silme İşlemi)
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var appointment = await _context.Appointments.FindAsync(id);

        if (appointment != null)
        {
            _context.Appointments.Remove(appointment);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Randevu #{id} başarıyla silindi.";
        }

        return RedirectToAction(nameof(Index));
    }
}