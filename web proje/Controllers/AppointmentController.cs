using FitnessCenterProject.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FitnessCenterProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;

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

        // ----------------------- READ (Index) - Randevuları Listeleme -----------------------
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // GÜVENLİK İYİLEŞTİRMESİ: Sadece oturum açan kullanıcının randevularını listeliyoruz.
            var appointments = await _context.Appointments
                .Include(a => a.Trainer)
                .Include(a => a.Service)
                .Where(a => a.UserId == userId) // Sadece kullanıcının kendi randevularını getir.
                .OrderByDescending(a => a.StartTime)
                .ToListAsync();

            return View(appointments);
        }

        // ----------------------- CREATE (Randevu Formu) -----------------------
        // GET: Appointment/Create
        public IActionResult Create()
        {
            ViewData["TrainerId"] = new SelectList(_context.Trainers, "TrainerId", "Name");
            ViewData["ServiceId"] = new SelectList(_context.Services, "ServiceId", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("TrainerId,ServiceId,StartTime,EndTime,Notes")] Appointment appointment)
        {
            ViewData["TrainerId"] = new SelectList(_context.Trainers, "TrainerId", "Name", appointment.TrainerId);
            ViewData["ServiceId"] = new SelectList(_context.Services, "ServiceId", "Name", appointment.ServiceId);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                ModelState.AddModelError(string.Empty, "Randevu oluşturmak için oturum açmalısınız.");
                return View(appointment);
            }
            appointment.UserId = userId;
            appointment.IsConfirmed = false;

            // 1. ADIM: GENEL MODEL DOĞRULAMASI
            if (!ModelState.IsValid)
            {
                return View(appointment);
            }

            // 2. ADIM: MANTIKSAL KONTROLLER VE ÇAKIŞMA KONTROLÜ
            if (appointment.StartTime <= DateTime.Now)
            {
                ModelState.AddModelError("StartTime", "Randevu başlangıç zamanı geçmiş bir tarih/saat olamaz.");
                return View(appointment);
            }

            if (appointment.StartTime >= appointment.EndTime)
            {
                ModelState.AddModelError("EndTime", "Bitiş zamanı başlangıç zamanından sonra olmalıdır.");
                return View(appointment);
            }

            // Eğitmen Çakışma Kontrolü
            var isConflicting = await _context.Appointments
                .AnyAsync(a =>
                    a.TrainerId == appointment.TrainerId &&
                    (
                        (appointment.StartTime < a.EndTime && appointment.StartTime >= a.StartTime) ||
                        (appointment.EndTime > a.StartTime && appointment.EndTime <= a.EndTime) ||
                        (appointment.StartTime <= a.StartTime && appointment.EndTime >= a.EndTime)
                    ));

            if (isConflicting)
            {
                ModelState.AddModelError(string.Empty, "Seçilen eğitmenin bu saatler arasında başka bir randevusu bulunmaktadır. Lütfen farklı bir saat seçiniz.");
                return View(appointment);
            }

            // 3. ADIM: KAYIT
            try
            {
                _context.Add(appointment);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Randevunuz başarıyla oluşturuldu!";
                return RedirectToAction(nameof(Index), "Appointment");
            }
            catch (DbUpdateException dbEx)
            {
                string errorMessage = "Randevu kaydedilirken bir veritabanı kısıtlama hatası oluştu.";
                if (dbEx.InnerException != null)
                {
                    errorMessage += " Detay: " + dbEx.InnerException.Message;
                }
                ModelState.AddModelError(string.Empty, errorMessage);
                return View(appointment);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Beklenmedik bir hata oluştu: " + ex.Message);
                return View(appointment);
            }
        }

        // ====================================================================
        // YENİ EKLENEN KOD: DÜZENLEME (EDIT)
        // ====================================================================

        // ----------------------- UPDATE (Düzenleme Formu) -----------------------
        // GET: Appointment/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var appointment = await _context.Appointments.FindAsync(id);

            if (appointment == null)
            {
                return NotFound();
            }

            // YETKİLENDİRME KONTROLÜ: Sadece randevuyu oluşturan kişi düzenleyebilir
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (appointment.UserId != userId)
            {
                return Forbid(); // 403 Forbidden
            }

            ViewData["TrainerId"] = new SelectList(_context.Trainers, "TrainerId", "Name", appointment.TrainerId);
            ViewData["ServiceId"] = new SelectList(_context.Services, "ServiceId", "Name", appointment.ServiceId);

            return View(appointment);
        }

        // ----------------------- UPDATE (Değişiklikleri Kaydetme) -----------------------
        // POST: Appointment/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("AppointmentId,TrainerId,ServiceId,StartTime,EndTime,Notes,UserId,IsConfirmed")] Appointment appointment)
        {
            if (id != appointment.AppointmentId)
            {
                return NotFound();
            }

            // Kullanıcının UserId'sini kontrol etme (Güvenlik Önlemi)
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (appointment.UserId != userId)
            {
                return Forbid();
            }

            // Randevunun UserId'sini URL'den gelen ID'ye eşitleyelim (Bind'den geliyor)
            var existingAppointment = await _context.Appointments.AsNoTracking().FirstOrDefaultAsync(a => a.AppointmentId == id);
            if (existingAppointment == null || existingAppointment.UserId != userId)
            {
                // Eğer randevu yoksa veya kullanıcıya ait değilse, bu bir güvenlik ihlalidir.
                return Forbid();
            }
            appointment.UserId = existingAppointment.UserId; // Mevcut kullanıcı ID'sini koru

            if (!ModelState.IsValid)
            {
                // Hata varsa View'a geri dönmeden önce dropdown listelerini tekrar doldur
                ViewData["TrainerId"] = new SelectList(_context.Trainers, "TrainerId", "Name", appointment.TrainerId);
                ViewData["ServiceId"] = new SelectList(_context.Services, "ServiceId", "Name", appointment.ServiceId);
                return View(appointment);
            }

            // MANTIKSAL KONTROLLER
            if (appointment.StartTime <= DateTime.Now)
            {
                ModelState.AddModelError("StartTime", "Randevu başlangıç zamanı geçmiş bir tarih/saat olamaz.");
            }

            if (appointment.StartTime >= appointment.EndTime)
            {
                ModelState.AddModelError("EndTime", "Bitiş zamanı başlangıç zamanından sonra olmalıdır.");
            }

            // Eğitmen Çakışma Kontrolü (Düzenlenen randevuyu hariç tutarak)
            var isConflicting = await _context.Appointments
                .AnyAsync(a =>
                    a.AppointmentId != id && // DÜZENLENEN randevuyu kontrol dışı bırak
                    a.TrainerId == appointment.TrainerId &&
                    (
                        (appointment.StartTime < a.EndTime && appointment.StartTime >= a.StartTime) ||
                        (appointment.EndTime > a.StartTime && appointment.EndTime <= a.EndTime) ||
                        (appointment.StartTime <= a.StartTime && appointment.EndTime >= a.EndTime)
                    ));

            if (isConflicting)
            {
                ModelState.AddModelError(string.Empty, "Seçilen eğitmenin bu saatler arasında başka bir randevusu bulunmaktadır. Lütfen farklı bir saat seçiniz.");
            }

            if (!ModelState.IsValid)
            {
                ViewData["TrainerId"] = new SelectList(_context.Trainers, "TrainerId", "Name", appointment.TrainerId);
                ViewData["ServiceId"] = new SelectList(_context.Services, "ServiceId", "Name", appointment.ServiceId);
                return View(appointment);
            }

            // Kayıt İşlemi
            try
            {
                _context.Update(appointment);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Randevunuz başarıyla güncellendi!";
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
            return RedirectToAction(nameof(Index));
        }

        // ----------------------- READ (Detaylar) -----------------------
        // GET: Appointment/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var appointment = await _context.Appointments
                .Include(a => a.Trainer) // Eğitmen detaylarını yükle
                .Include(a => a.Service) // Hizmet detaylarını yükle
                .FirstOrDefaultAsync(m => m.AppointmentId == id);

            if (appointment == null)
            {
                return NotFound();
            }

            // YETKİLENDİRME KONTROLÜ: Sadece randevuyu oluşturan kişi görebilir
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (appointment.UserId != userId)
            {
                return Forbid();
            }

            return View(appointment);
        }

        // ====================================================================
        // YENİ EKLENEN KOD: SİLME (DELETE)
        // ====================================================================

        // ----------------------- DELETE (Onay Formu) -----------------------
        // GET: Appointment/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var appointment = await _context.Appointments
                .Include(a => a.Trainer)
                .Include(a => a.Service)
                .FirstOrDefaultAsync(m => m.AppointmentId == id);

            if (appointment == null)
            {
                return NotFound();
            }

            // YETKİLENDİRME KONTROLÜ
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (appointment.UserId != userId)
            {
                return Forbid();
            }

            return View(appointment);
        }

        // ----------------------- DELETE (Silme İşlemi) -----------------------
        // POST: Appointment/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);

            if (appointment != null)
            {
                // YETKİLENDİRME KONTROLÜ
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (appointment.UserId != userId)
                {
                    return Forbid();
                }

                _context.Appointments.Remove(appointment);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Randevunuz başarıyla iptal edildi!";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}