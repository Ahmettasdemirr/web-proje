using FitnessCenterProject.Data;
using FitnessCenterProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace FitnessCenterProject.Controllers
{
    [Authorize(Roles = "Admin")]
    public class TrainerController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TrainerController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Trainer (Eğitmen Listesi)
        public async Task<IActionResult> Index()
        {
            var trainers = await _context.Trainers
                .Include(t => t.TrainerServices)
                .ThenInclude(ts => ts.Service)
                .ToListAsync();

            return View(trainers);
        }

        // GET: Trainer/Create
        public IActionResult Create()
        {
            ViewData["AllServices"] = _context.Services.ToList();
            return View();
        }

        // POST: Trainer/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        // KRİTİK: Bind'den ImageUrl kaldırıldı.
        public async Task<IActionResult> Create([Bind("TrainerId,Name")] Trainer trainer, int[] selectedServiceIds)
        {
            if (ModelState.IsValid)
            {
                trainer.TrainerServices = selectedServiceIds
                    .Select(id => new TrainerService { ServiceId = id })
                    .ToList();

                _context.Add(trainer);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"{trainer.Name} adlı eğitmen başarıyla eklendi.";
                return RedirectToAction(nameof(Index));
            }

            ViewData["AllServices"] = _context.Services.ToList();
            return View(trainer);
        }

        // GET: Trainer/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var trainer = await _context.Trainers
                .Include(t => t.TrainerServices)
                .FirstOrDefaultAsync(m => m.TrainerId == id);

            if (trainer == null) return NotFound();

            ViewData["AllServices"] = _context.Services.ToList();
            ViewData["SelectedServiceIds"] = trainer.TrainerServices.Select(ts => ts.ServiceId).ToList();

            return View(trainer);
        }

        // POST: Trainer/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        // KRİTİK: Bind'den ImageUrl kaldırıldı.
        public async Task<IActionResult> Edit(int id, [Bind("TrainerId,Name")] Trainer trainer, int[] selectedServiceIds)
        {
            if (id != trainer.TrainerId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var existingServices = await _context.TrainerServices
                        .Where(ts => ts.TrainerId == trainer.TrainerId)
                        .ToListAsync();
                    _context.TrainerServices.RemoveRange(existingServices);

                    var newServices = selectedServiceIds
                        .Select(id => new TrainerService { TrainerId = trainer.TrainerId, ServiceId = id })
                        .ToList();
                    _context.TrainerServices.AddRange(newServices);

                    _context.Update(trainer);

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"{trainer.Name} bilgileri başarıyla güncellendi.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Trainers.Any(e => e.TrainerId == id))
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

            ViewData["AllServices"] = _context.Services.ToList();
            ViewData["SelectedServiceIds"] = selectedServiceIds.ToList();
            return View(trainer);
        }

        // Trainer/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var trainer = await _context.Trainers
                .Include(t => t.TrainerServices)
                .ThenInclude(ts => ts.Service)
                .FirstOrDefaultAsync(m => m.TrainerId == id);

            if (trainer == null) return NotFound();

            return View(trainer);
        }

        // Trainer/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var trainer = await _context.Trainers
                .Include(t => t.TrainerServices)
                .ThenInclude(ts => ts.Service)
                .FirstOrDefaultAsync(m => m.TrainerId == id);

            if (trainer == null) return NotFound();

            return View(trainer);
        }

        // POST: Trainer/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var trainer = await _context.Trainers
                .Include(t => t.TrainerServices)
                .FirstOrDefaultAsync(t => t.TrainerId == id);

            if (trainer != null)
            {
                _context.Trainers.Remove(trainer);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Eğitmen başarıyla silindi.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}