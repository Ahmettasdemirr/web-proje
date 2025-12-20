using FitnessCenterProject.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using FitnessCenterProject.Models;

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
            // Bu metot değiştirilmedi.
            var trainers = await _context.Trainers
                .Include(t => t.TrainerServices)
                .ThenInclude(ts => ts.Service)
                .Select(t => new
                {
                    TrainerId = t.TrainerId,
                    Name = t.Name,
                    SpecialtyText = string.Join(", ", t.TrainerServices.Select(ts => ts.Service.Name)),
                })
                .ToListAsync();

            if (trainers == null || !trainers.Any())
            {
                return NotFound("Sistemde kayıtlı eğitmen bulunamadı.");
            }

            return Ok(trainers);
        }

        // GET: api/TrainersApi/available?date=...&startTime=...&duration=...&serviceId=...
        [HttpGet("available")]
        public async Task<IActionResult> GetAvailableTrainers(string date, string startTime, int duration, int serviceId)
        {
            // ... (Tarih, saat ve süre kontrolleri aynı kalır) ...
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
                return NotFound("Geçerli bir Hizmet ID'si (serviceId) belirtilmelidir veya bu hizmet bulunamadı.");
            }


            var appointmentEndTime = appointmentStartTime.AddMinutes(duration);

            if (appointmentStartTime < System.DateTime.Now)
            {
                return BadRequest("Randevu başlangıç zamanı geçmiş bir tarih/saat olamaz.");
            }

            // KRİTİK ÇÖZÜM: İlk aşamada sadece gerekli verileri çekiyoruz (SQL'de çalışacak kısım)
            // Ardından ToList() ile veriyi belleğe çekip (.AsEnumerable()) kompleks işlemleri (string.Join) bellekte yapıyoruz.

            var allTrainersWithDetails = await _context.Trainers
                .Include(t => t.TrainerServices).ThenInclude(ts => ts.Service)
                .Include(t => t.Appointments)
                .ToListAsync(); // Veriyi belleğe çekeriz


            // BELLEK ÜZERİNDE FİLTRELEME VE DÖNÜŞTÜRME
            var availableTrainers = allTrainersWithDetails
                // 1. Randevu Çakışması Kontrolü (Müsaitlik)
                .Where(t => !t.Appointments.Any(a =>
                    (appointmentStartTime < a.EndTime && appointmentStartTime >= a.StartTime) ||
                    (appointmentEndTime > a.StartTime && appointmentEndTime <= a.EndTime) ||
                    (appointmentStartTime <= a.StartTime && appointmentEndTime >= a.EndTime)
                ))

                // 2. Projeksiyon (string.Join burada güvenle çalışır)
                .Select(t => new TrainerApiResult
                {
                    TrainerId = t.TrainerId,
                    Name = t.Name,
                    // Artık string.Join güvenle çalışır:
                    Specialty = string.Join(", ", t.TrainerServices?.Select(ts => ts.Service?.Name) ?? new List<string>()),
                    Availability = $"{appointmentStartTime:dd.MM.yyyy HH:mm} - {appointmentEndTime:HH:mm}"
                })
                .ToList();


            if (!availableTrainers.Any())
            {
                return NotFound("Belirtilen saat aralığında müsait antrenör bulunmamaktadır.");
            }

            return Ok(availableTrainers);
        }
    }
}