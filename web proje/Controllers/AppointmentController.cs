using FitnessCenterProject.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FitnessCenterProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;
using System.Globalization;
using System.Linq;

namespace FitnessCenterProject.Controllers
{
    [Authorize]
    public class AppointmentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AppointmentController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Randevuları Listeleme
        public async Task<IActionResult> Index()
        {
            // Admin rolündeyse Admin sayfasına yönlendir
            if (User.IsInRole("Admin"))
            {
                return RedirectToAction("Index", "AdminAppointment");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var appointments = await _context.Appointments
                .Include(a => a.Trainer)
                .Include(a => a.Service)
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.StartTime)
                .ToListAsync();

            return View(appointments);
        }

        // GET: Randevu Oluşturma Formu
        public IActionResult Create()
        {
            ViewData["ServiceId"] = new SelectList(_context.Services, "ServiceId", "Name");
            return View();
        }

        // POST: Randevu Oluşturma (Kullanıcı Eğitmeni Kendisi Seçer)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ServiceId,TrainerId,Notes")] Appointment appointment, [FromForm] string StartTime)
        {
            // View için SelectList'i doldur
            ViewData["ServiceId"] = new SelectList(_context.Services, "ServiceId", "Name", appointment.ServiceId);

            // TrainerId'nin seçili olduğundan emin ol
            if (appointment.TrainerId == 0)
            {
                ModelState.AddModelError("TrainerId", "Lütfen müsait eğitmen listesinden bir seçim yapınız.");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                ModelState.AddModelError(string.Empty, "Randevu oluşturmak için oturum açmalısınız.");
                return View(appointment);
            }

            // StartTime string'ini DateTime nesnesine dönüştürme
            DateTime parsedStartTime;
            const string expectedFormat = "yyyy-MM-ddTHH:mm:ss";

            if (!DateTime.TryParseExact(StartTime, expectedFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedStartTime))
            {
                if (!DateTime.TryParseExact(StartTime, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedStartTime))
                {
                    ModelState.AddModelError("StartTime", "Geçerli bir tarih ve saat formatı giriniz.");
                    return View(appointment);
                }
            }

            appointment.StartTime = parsedStartTime;

            // EndTime'ı hesapla
            var service = await _context.Services.FindAsync(appointment.ServiceId);
            if (service == null)
            {
                ModelState.AddModelError("ServiceId", "Seçilen hizmet bulunamadı.");
                return View(appointment);
            }
            appointment.EndTime = appointment.StartTime.AddMinutes(service.DurationMinutes);

            // Mantıksal Kontroller
            if (appointment.StartTime <= DateTime.Now)
            {
                ModelState.AddModelError("StartTime", "Randevu başlangıç zamanı geçmiş bir tarih/saat olamaz.");
            }

            if (ModelState.IsValid)
            {
                // --- KRİTİK: Eğitmen Müsaitlik Kontrolü ---

                var trainer = await _context.Trainers
                    .Include(t => t.Appointments)
                    .Include(t => t.TrainerServices)
                    .FirstOrDefaultAsync(t => t.TrainerId == appointment.TrainerId);

                if (trainer == null || !trainer.TrainerServices.Any(ts => ts.ServiceId == appointment.ServiceId))
                {
                    ModelState.AddModelError("TrainerId", "Seçilen eğitmen bu hizmeti vermeye uygun değil.");
                    return View(appointment);
                }

                // Müsaitlik Kontrolü
                // Randevu çakışması kontrolü
                bool isConflicting = trainer.Appointments.Any(a =>
                    (appointment.StartTime < a.EndTime && appointment.StartTime >= a.StartTime) ||
                    (appointment.EndTime > a.StartTime && appointment.EndTime <= a.EndTime) ||
                    (appointment.StartTime <= a.StartTime && appointment.EndTime >= a.EndTime));

                if (isConflicting)
                {
                    ModelState.AddModelError("TrainerId", $"{trainer.Name} bu saat aralığında maalesef müsait değil. Lütfen listeden başka bir eğitmen seçin.");
                    goto SkipSave;
                }

                // Kayıt İşlemi
                appointment.UserId = userId;
                appointment.IsConfirmed = false; // İlk randevu yönetici onayı gerektirir.

                try
                {
                    _context.Add(appointment);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"{trainer.Name} adlı eğitmene başarılı randevu atandı ve yönetici onayına sunuldu!";
                    return RedirectToAction(nameof(Index), "Appointment");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, "Beklenmedik bir hata oluştu: " + ex.Message);
                    goto SkipSave;
                }
            }

        SkipSave:
            return View(appointment);
        }

        // **Randevu Düzenleme (Edit) Action'ları KALDIRILDI**

        // --- Detaylar ---

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var appointment = await _context.Appointments
                .Include(a => a.Trainer)
                .Include(a => a.Service)
                .FirstOrDefaultAsync(m => m.AppointmentId == id);

            if (appointment == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            // Kendi randevusu değilse veya Admin değilse erişimi engelle
            if (appointment.UserId != userId && !User.IsInRole("Admin")) return Forbid();

            return View(appointment);
        }

        // --- Randevu Silme ---

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var appointment = await _context.Appointments
                .Include(a => a.Trainer)
                .Include(a => a.Service)
                .FirstOrDefaultAsync(m => m.AppointmentId == id);

            if (appointment == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            // Kendi randevusu değilse veya Admin değilse erişimi engelle
            if (appointment.UserId != userId && !User.IsInRole("Admin")) return Forbid();

            return View(appointment);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);

            if (appointment != null)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                // Kendi randevusu değilse veya Admin değilse silmeyi engelle
                if (appointment.UserId != userId && !User.IsInRole("Admin")) return Forbid();

                _context.Appointments.Remove(appointment);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Randevunuz başarıyla iptal edildi!";
            }

            // Admin siliyorsa Admin sayfasına, kullanıcı siliyorsa kendi listesine yönlendir
            return User.IsInRole("Admin") ? RedirectToAction("Index", "AdminAppointment") : RedirectToAction(nameof(Index));
        }
    }
}