using FitnessCenterProject.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace FitnessCenterProject.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TrainersApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TrainersApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/TrainersApi (Tüm Eğitmenleri listeler)
        [HttpGet]
        public async Task<IActionResult> GetTrainers()
        {
            // Eğitmenleri, ilişkili TrainerServices ve Service bilgileriyle birlikte çekiyoruz.
            var trainers = await _context.Trainers
                .Include(t => t.TrainerServices)
                .ThenInclude(ts => ts.Service)
                .Select(t => new
                {
                    TrainerId = t.TrainerId,
                    Name = t.Name,
                    // 💡 GÜNCELLEME: Specialty yerine Hizmet isimlerini birleştiriyoruz.
                    Specialties = t.TrainerServices.Select(ts => ts.Service.Name).ToList(),
                    SpecialtyText = string.Join(", ", t.TrainerServices.Select(ts => ts.Service.Name)),
                })
                .ToListAsync();

            if (trainers == null || !trainers.Any())
            {
                return NotFound("Sistemde kayıtlı eğitmen bulunamadı.");
            }

            return Ok(trainers);
        }


        // GET: api/TrainersApi/available?date=2025-12-25&startTime=14:00&duration=60&serviceId=1
        // Belirli bir tarih, saat aralığında ve BELİRLİ BİR HİZMETİ verebilecek müsait antrenörleri döndürür.
        [HttpGet("available")]
        public async Task<IActionResult> GetAvailableTrainers(string date, string startTime, int duration, int serviceId)
        {
            // 1. Tarih ve Saat Girdilerini Birleştirme ve Kontrol Etme
            if (!System.DateTime.TryParse($"{date} {startTime}", out System.DateTime appointmentStartTime))
            {
                return BadRequest("Geçerli bir tarih (YYYY-MM-DD) ve başlangıç saati (HH:mm) formatı giriniz.");
            }

            if (duration <= 0)
            {
                return BadRequest("Süre (duration) pozitif bir değer olmalıdır.");
            }

            var service = await _context.Services.FindAsync(serviceId);
            if (service == null)
            {
                return NotFound("Geçerli bir Hizmet ID'si (serviceId) belirtilmelidir.");
            }


            var appointmentEndTime = appointmentStartTime.AddMinutes(duration);

            if (appointmentStartTime < System.DateTime.Now)
            {
                return BadRequest("Randevu başlangıç zamanı geçmiş bir tarih/saat olamaz.");
            }

            // 2. Çakışma Kontrolü ve Hizmet Uygunluğu Kontrolü

            // Seçilen HİZMETİ verebilecek tüm eğitmenleri çek
            var qualifiedTrainers = await _context.Trainers
                .Include(t => t.TrainerServices).ThenInclude(ts => ts.Service)
                .Include(t => t.Appointments)
                .Where(t => t.TrainerServices.Any(ts => ts.ServiceId == serviceId)) // 💡 KRİTİK FİLTRE: Hizmet uygunluğu
                .ToListAsync();


            // 3. Müsait Eğitmenleri Filtreleme (Hizmet uygunluğu kontrol edilmiş listeyi kullanıyoruz)
            var availableTrainers = qualifiedTrainers
                .Where(t => !t.Appointments.Any(a =>
                    // Çakışma kontrolü
                    (appointmentStartTime < a.EndTime && appointmentStartTime >= a.StartTime) ||
                    (appointmentEndTime > a.StartTime && appointmentEndTime <= a.EndTime) ||
                    (appointmentStartTime <= a.StartTime && appointmentEndTime >= a.EndTime)
                ))
                .Select(t => new
                {
                    t.TrainerId,
                    t.Name,
                    // 💡 GÜNCELLEME: Hizmet isimlerini Select ediyoruz
                    SpecialtyText = string.Join(", ", t.TrainerServices.Select(ts => ts.Service.Name)),
                    Availability = $"{appointmentStartTime:dd.MM.yyyy HH:mm} - {appointmentEndTime:HH:mm}"
                })
                .ToList();


            if (!availableTrainers.Any())
            {
                return NotFound($"Seçilen hizmet ({service.Name}) için, belirtilen saat aralığında müsait antrenör bulunmamaktadır.");
            }

            return Ok(availableTrainers);
        }
    }
}