using FitnessCenterProject.Data; // DbContext'i kullanmak için
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // ToListAsync() için
using FitnessCenterProject.Models; // Trainer modelini kullanmak için
using Microsoft.AspNetCore.Authorization;

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

       
        public async Task<IActionResult> Index()
        {
            // Veritabanındaki tüm Trainer kayıtlarını asenkron olarak çeker
            var trainers = await _context.Trainers.ToListAsync();

            // Çekilen listeyi (Model) View'e gönderir
            return View(trainers);
        }

        public IActionResult Create()
        {
            // Boş bir form göndereceğiz
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name")] Trainer trainer) // Sadece Name alanını alıyoruz
        {
            // Veri Doğrulama (Data Validation) kontrolü
            if (ModelState.IsValid)
            {
                // Antrenörü veritabanına ekle
                _context.Add(trainer);
                await _context.SaveChangesAsync();

                // Kayıt başarılıysa, antrenör listesine yönlendir
                return RedirectToAction(nameof(Index));
            }

            // Veri doğrulama hatası varsa, kullanıcıya hatayı göstermek için view'i tekrar gönder
            return View(trainer);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound(); // ID yoksa 404
            }

            // ID'ye göre antrenörü veritabanından bul
            var trainer = await _context.Trainers.FindAsync(id);

            if (trainer == null)
            {
                return NotFound(); // Antrenör bulunamazsa 404
            }
            return View(trainer);
        }

        // POST: Trainer/Edit/5
        // Düzenlenmiş veriyi alır ve veritabanında günceller
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("TrainerId,Name")] Trainer trainer)
        {
            // URL'deki ID ile formdan gelen ID eşleşmiyorsa hata
            if (id != trainer.TrainerId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(trainer); // Değişiklikleri işaretle
                    await _context.SaveChangesAsync(); // Veritabanına kaydet
                }
                catch (DbUpdateConcurrencyException)
                {
                    // Eşzamanlılık (Concurrency) kontrolü: Kayıt başkası tarafından silinmiş mi?
                    if (!_context.Trainers.Any(e => e.TrainerId == trainer.TrainerId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw; // Başka bir hata varsa fırlat
                    }
                }
                return RedirectToAction(nameof(Index)); // Listeleme sayfasına dön
            }
            return View(trainer); // Doğrulama hatası varsa formu tekrar göster
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var trainer = await _context.Trainers
                .FirstOrDefaultAsync(m => m.TrainerId == id);

            if (trainer == null)
            {
                return NotFound();
            }

            return View(trainer);
        }

        // POST: Trainer/Delete/5
        // Silme işlemini onaylar ve kaydı veritabanından kaldırır
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var trainer = await _context.Trainers.FindAsync(id);

            if (trainer != null)
            {
                _context.Trainers.Remove(trainer); // Kaldırma işlemini işaretle
            }

            await _context.SaveChangesAsync(); // Veritabanına kaydet
            return RedirectToAction(nameof(Index)); // Listeleme sayfasına dön
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound(); // ID yoksa 404
            }

            // ID'ye göre antrenörü veritabanından bul
            var trainer = await _context.Trainers
                .FirstOrDefaultAsync(m => m.TrainerId == id);

            if (trainer == null)
            {
                return NotFound(); // Antrenör bulunamazsa 404
            }

            return View(trainer); // Bulunan antrenör nesnesini View'e gönder
        }
    }
}