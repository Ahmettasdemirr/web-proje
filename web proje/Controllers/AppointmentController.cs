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
            // View'de AJAX ile doldurulacağı için TrainerId SelectList'i burada boş bırakılır.
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
            if (!DateTime.TryParseExact(StartTime, "dd.MM.yyyy HH:mm", CultureInfo.GetCultureInfo("tr-TR"), DateTimeStyles.None, out parsedStartTime))
            {
                ModelState.AddModelError("StartTime", "Geçerli bir tarih ve saat formatı giriniz (Örn: 18.12.2025 14:30).");
                return View(appointment);
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
                // --- KRİTİK: Kullanıcının Seçtiği Eğitmen Müsait mi Kontrolü ---

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
                bool isConflicting = trainer.Appointments.Any(a =>
                    (appointment.StartTime < a.EndTime && appointment.StartTime >= a.StartTime) ||
                    (appointment.EndTime > a.StartTime && appointment.EndTime <= a.EndTime) ||
                    (appointment.StartTime <= a.StartTime && appointment.EndTime >= a.EndTime));

                if (isConflicting)
                {
                    ModelState.AddModelError("TrainerId", $"{trainer.Name} bu saat aralığında maalesef müsait değil. Lütfen listeden başka bir eğitmen seçin.");
                    goto SkipSave; // Hata durumunda kayıt işlemini atla
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
            // Model geçerli değilse veya atama başarısız olursa
            return View(appointment);
        }

        // GET: Randevu Düzenleme Formu
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var appointment = await _context.Appointments
                .Include(a => a.Trainer)
                .Include(a => a.Service)
                .FirstOrDefaultAsync(m => m.AppointmentId == id);

            if (appointment == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (appointment.UserId != userId) return Forbid();

            // Edit View'de de Trainer seçimi AJAX ile yapılacak
            ViewData["ServiceId"] = new SelectList(_context.Services, "ServiceId", "Name", appointment.ServiceId);

            return View(appointment);
        }

        // POST: Randevuyu Kaydetme (Düzenlemede Kullanıcının Seçtiği Eğitmenin Müsaitliği Kontrol Edilir)
        // POST: Randevuyu Kaydetme (Düzenlemede Kullanıcının Seçtiği Eğitmenin Müsaitliği Kontrol Edilir)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("AppointmentId,ServiceId,TrainerId,Notes")] Appointment appointment, [FromForm] string StartTime)
        {
            if (id != appointment.AppointmentId) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Mevcut randevuyu SADECE OKUMAK ve takip mekanizmasını bozmamak için AsNoTracking() kullanıyoruz.
            var existingAppointment = await _context.Appointments.AsNoTracking().FirstOrDefaultAsync(a => a.AppointmentId == id);

            if (existingAppointment == null || existingAppointment.UserId != userId) return Forbid();

            // View için SelectList'i doldur (Hata olsa bile geri dönebilmek için)
            ViewData["ServiceId"] = new SelectList(_context.Services, "ServiceId", "Name", appointment.ServiceId);

            // --- StartTime Dönüşümü ve Hesaplama ---
            DateTime parsedStartTime;
            if (!DateTime.TryParseExact(StartTime, "dd.MM.yyyy HH:mm", CultureInfo.GetCultureInfo("tr-TR"), DateTimeStyles.None, out parsedStartTime))
            {
                ModelState.AddModelError("StartTime", "Geçerli bir tarih ve saat formatı giriniz (Örn: 18.12.2025 14:30).");
                return View(appointment);
            }

            appointment.StartTime = parsedStartTime;

            var service = await _context.Services.FindAsync(appointment.ServiceId);
            if (service == null)
            {
                ModelState.AddModelError("ServiceId", "Seçilen hizmet bulunamadı.");
                return View(appointment);
            }
            appointment.EndTime = appointment.StartTime.AddMinutes(service.DurationMinutes);

            // Mantıksal Kontrol
            if (appointment.StartTime <= DateTime.Now)
            {
                ModelState.AddModelError("StartTime", "Randevu başlangıç zamanı geçmiş bir tarih/saat olamaz.");
            }

            // Eğer eğitmen seçilmemişse, mevcut eğitmeni kullan (sadece Notlar değiştiğinde)
            if (appointment.TrainerId == 0)
            {
                appointment.TrainerId = existingAppointment.TrainerId;
            }
            // Hata kontrolü (hala sıfırsa hata ver)
            if (appointment.TrainerId == 0)
            {
                ModelState.AddModelError("TrainerId", "Lütfen müsait eğitmen listesinden bir seçim yapınız.");
            }

            // Model geçerli değilse, hemen geri dön
            if (!ModelState.IsValid)
            {
                return View(appointment);
            }

            // KRİTİK ALANLARDA DEĞİŞİKLİK KONTROLÜ
            bool timeOrServiceOrTrainerChanged =
                appointment.ServiceId != existingAppointment.ServiceId ||
                appointment.StartTime != existingAppointment.StartTime ||
                appointment.TrainerId != existingAppointment.TrainerId;

            // --- Müsaitlik Kontrolü ---
            // TRAINER ÇEKİMİNDE DE ASNOTRACKING KULLANILDI
            var trainer = await _context.Trainers
                .Include(t => t.Appointments)
                .AsNoTracking() // Takip hatasını önlemek için burada da ekledik
                .FirstOrDefaultAsync(t => t.TrainerId == appointment.TrainerId);

            // CS8602 Düzeltmesi (Eğitmen bulunamazsa)
            if (trainer == null)
            {
                ModelState.AddModelError("TrainerId", "Seçilen eğitmen bulunamadı.");
                return View(appointment);
            }

            bool isConflicting = trainer.Appointments.Any(a =>
                a.AppointmentId != appointment.AppointmentId && // Kendisini hariç tut
                ((appointment.StartTime < a.EndTime && appointment.StartTime >= a.StartTime) ||
                (appointment.EndTime > a.StartTime && appointment.EndTime <= a.EndTime) ||
                (appointment.StartTime <= a.StartTime && appointment.EndTime >= a.EndTime)));

            if (isConflicting)
            {
                ModelState.AddModelError("TrainerId", $"{trainer.Name} bu saat aralığında maalesef müsait değil. Lütfen listeden başka bir eğitmen seçin.");
                return View(appointment);
            }

            // --- Güncelleme ve Onay Yönetimi ---

            // Kullanıcı ID'si ve eski onay durumu korunur.
            appointment.UserId = existingAppointment.UserId;
            appointment.IsConfirmed = existingAppointment.IsConfirmed;

            // Değişiklik olduysa onayı sıfırla
            if (timeOrServiceOrTrainerChanged)
            {
                appointment.IsConfirmed = false;
                TempData["WarningMessage"] = $"Randevunuzun bilgileri değiştirildiği için **yönetici onayı tekrar gerekmektedir!**";
            }

            // Kayıt İşlemi
            try
            {
                // HATA ÇÖZÜMÜ BURADA: _context.Update() metodu, takipsiz nesneyi Modified olarak işaretler.
                _context.Update(appointment);
                await _context.SaveChangesAsync();

                if (TempData["WarningMessage"] == null)
                {
                    TempData["SuccessMessage"] = "Randevunuz başarıyla güncellendi!";
                }
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

        // --- Detaylar ve Silme action'ları ---

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var appointment = await _context.Appointments
                .Include(a => a.Trainer)
                .Include(a => a.Service)
                .FirstOrDefaultAsync(m => m.AppointmentId == id);

            if (appointment == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (appointment.UserId != userId && !User.IsInRole("Admin")) return Forbid();

            return View(appointment);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var appointment = await _context.Appointments
                .Include(a => a.Trainer)
                .Include(a => a.Service)
                .FirstOrDefaultAsync(m => m.AppointmentId == id);

            if (appointment == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
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
                if (appointment.UserId != userId && !User.IsInRole("Admin")) return Forbid();

                _context.Appointments.Remove(appointment);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Randevunuz başarıyla iptal edildi!";
            }

            return User.IsInRole("Admin") ? RedirectToAction("Index", "AdminAppointment") : RedirectToAction(nameof(Index));
        }
    }
}