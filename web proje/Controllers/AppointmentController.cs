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

        // ----------------------- READ (Index) - Şimdilik Boş -----------------------
        public async Task<IActionResult> Index()
        {
            // Randevularla birlikte (eğer View'ınızda kullanılıyorsa) Trainer ve Service verilerini de çekiyoruz.
            var appointments = await _context.Appointments
                .Include(a => a.Trainer)
                .Include(a => a.Service)
                // Eğer sadece o anki kullanıcının randevularını göstermek istiyorsanız bu satırı ekleyin:
                // .Where(a => a.UserId == User.FindFirstValue(ClaimTypes.NameIdentifier)) 
                .OrderByDescending(a => a.StartTime)
                .ToListAsync();

            return View(appointments); // Randevu listesini View'a gönderiyoruz.
        }

        // ----------------------- CREATE (Randevu Formu) -----------------------
        // GET: Appointment/Create
        public IActionResult Create()
        {
            ViewData["TrainerId"] = new SelectList(_context.Trainers, "TrainerId", "Name");
            ViewData["ServiceId"] = new SelectList(_context.Services, "ServiceId", "Name");
            return View();
        }

        // POST: Appointment/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("TrainerId,ServiceId,StartTime,EndTime,Notes")] Appointment appointment)
        {
            ViewData["TrainerId"] = new SelectList(_context.Trainers, "TrainerId", "Name", appointment.TrainerId);
            ViewData["ServiceId"] = new SelectList(_context.Services, "ServiceId", "Name", appointment.ServiceId);

            // Oturum açmış kullanıcının ID'sini randevuya ekleme
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
            catch (DbUpdateException dbEx) // Özellikle EF Core hatalarını yakala
            {
                // Veritabanı kısıtlaması (Foreign Key, Not Null) ihlalini kontrol et
                string errorMessage = "Randevu kaydedilirken bir veritabanı kısıtlama hatası oluştu.";

                // Asıl hatayı (PostgreSQL hatası) Inner Exception'da arıyoruz
                if (dbEx.InnerException != null)
                {
                    errorMessage += " Detay: " + dbEx.InnerException.Message;
                }

                // Kullanıcıya anlaşılır hatayı göster
                ModelState.AddModelError(string.Empty, errorMessage);

                // Geliştirme ortamında tam hatayı da konsola yazdırabilirsiniz.
                // Console.WriteLine(dbEx.ToString()); 

                return View(appointment);
            }
            catch (Exception ex)
            {
                // Diğer genel hatalar için
                ModelState.AddModelError(string.Empty, "Beklenmedik bir hata oluştu: " + ex.Message);
                return View(appointment);
            }
        }
    }
}