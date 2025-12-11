using FitnessCenterProject.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FitnessCenterProject.Models;
using Microsoft.AspNetCore.Authorization;

namespace FitnessCenterProject.Controllers
{
    // SADECE "Admin" rolüne sahip kullanıcılar erişebilir
    [Authorize(Roles = "Admin")]
    public class ServiceController : Controller
    {
        private readonly ApplicationDbContext _context;

        // Constructor: DbContext Enjeksiyonu
        public ServiceController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ----------------------- READ (Index) -----------------------
        // GET: Service/Index
        public IActionResult Index()
        {
            var services = _context.Services.ToList();
            return View(services);
        }

        // ----------------------- CREATE -----------------------
        // GET: Services/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Services/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ServiceId,Name,Description,Price,DurationMinutes")] Service service)
        {
            if (ModelState.IsValid)
            {
                _context.Add(service);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(service);
        }

        // ----------------------- DETAILS -----------------------
        // GET: Services/Details/5 (ID'ye göre hizmetin tüm detaylarını gösterir)
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Hizmeti ServiceId'ye göre bul
            var service = await _context.Services.FirstOrDefaultAsync(m => m.ServiceId == id);

            if (service == null)
            {
                return NotFound();
            }

            return View(service); // Detaylar sayfasını göster
        }

        // ----------------------- UPDATE (Edit) -----------------------
        // GET: Services/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var service = await _context.Services.FindAsync(id);
            if (service == null)
            {
                return NotFound();
            }
            return View(service);
        }

        // POST: Services/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ServiceId,Name,Description,Price,DurationMinutes")] Service service)
        {
            if (id != service.ServiceId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(service);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Services.Any(e => e.ServiceId == service.ServiceId))
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
            return View(service);
        }

        // ----------------------- DELETE -----------------------
        // GET: Services/Delete/5 (Silme onay sayfasını gösterir)
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var service = await _context.Services.FirstOrDefaultAsync(m => m.ServiceId == id);

            if (service == null)
            {
                return NotFound();
            }

            return View(service); // Silme onay sayfasını göster
        }

        // POST: Services/Delete/5 (Silme işlemini gerçekleştirir)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var service = await _context.Services.FindAsync(id);

            if (service != null)
            {
                _context.Services.Remove(service); // Hizmeti bağlamdan kaldır
                await _context.SaveChangesAsync(); // Değişiklikleri veritabanına kaydet (Silme)
            }

            return RedirectToAction(nameof(Index)); // Silme başarılı, Index sayfasına yönlendir
        }
    }
}