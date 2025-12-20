using FitnessCenterProject.Models;
using FitnessCenterProject.Models.Gemini;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net.Http.Json; // HttpClient JSON uzantıları için
using System.Net.Http.Headers;
using System; // Exception için gerekli

namespace FitnessCenterProject.Controllers
{
    public class AIController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        // HttpClient'ı yapılandırma (IConfiguration) ile alıyoruz
        public AIController(IConfiguration configuration)
        {
            _configuration = configuration;
            _httpClient = new HttpClient();
        }

        // GET: Formu Göster
        public IActionResult GeneratePlan()
        {
            var viewModel = new AIRequestViewModel();
            // Bu listeyi View'e göndermek için ViewModel'in içinde olması daha iyi practice'dir, ancak ViewBag de çalışır.
            ViewBag.Hedefler = new List<string> { "Kilo Verme", "Kas Geliştirme", "Dayanıklılık", "Esneklik" };
            return View(viewModel);
        }

        // POST: Planı Oluştur
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GeneratePlan(AIRequestViewModel model)
        {
            ViewBag.Hedefler = new List<string> { "Kilo Verme", "Kas Geliştirme", "Dayanıklılık", "Esneklik" };

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // 1. Prompt Oluşturma
            string prompt = $"Sen bir fitness koçusun ve diyetisyensin. Boyu {model.Boy} cm, kilosu {model.Kilo} kg olan ve {model.Hedef} hedefine ulaşmak isteyen, haftada {model.HaftalikEgzersizSayisi} kez egzersiz yapacak bir kullanıcı için 7 günlük detaylı bir Egzersiz ve Beslenme Planı oluştur. Beslenme planı için Türk mutfağına uygun ve gerçekçi önerilerde bulun. Yanıtı sadece temiz HTML formatında, `h3` başlıkları ve `ul/li` (madde işaretleri) kullanarak formatla. Ek Bilgileri dikkate al.";

            if (!string.IsNullOrEmpty(model.EkBilgiler))
            {
                prompt += $" Kullanıcının dikkate alması gereken ek notlar: {model.EkBilgiler}.";
            }

            // 2. API Key ve URL Hazırlığı
            var apiKey = _configuration.GetSection("Gemini").GetValue<string>("ApiKey");

            if (string.IsNullOrEmpty(apiKey))
            {
                ModelState.AddModelError(string.Empty, "Gemini API Anahtarı bulunamadı veya ayarlanmadı. Lütfen appsettings.json dosyanızdaki 'Gemini:ApiKey' ayarını kontrol edin.");
                return View(model);
            }

            // Gemini API'nin URL'si
            string apiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}";

            // 3. Gemini İstek Yapısını Oluşturma
            var request = new GeminiChatRequest
            {
                Contents = new List<Content>
                {
                    new Content
                    {
                        Role = "user",
                        Parts = new List<Part> { new Part { Text = prompt } }
                    }
                }
            };

            // 4. API Çağrısı
            try
            {
                var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(apiUrl, content);

                // ✅ KRİTİK DÜZELTME: Sonsuz yüklenme hatasını çözmek için IsSuccessStatusCode kontrolü
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();

                    // Hata mesajını kısa ve öz gösterme
                    var trimmedError = errorContent.Length > 200 ? errorContent.Substring(0, 200) + "..." : errorContent;

                    ModelState.AddModelError(string.Empty, $"Gemini API çağrısı başarısız oldu: {response.StatusCode} ({trimmedError})");

                    // Hata durumunda View'i hemen döndürerek sonsuz yüklenmeyi keser
                    return View(model);
                }

                // Başarılıysa yanıtı okumaya devam et
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<GeminiChatResponse>(jsonResponse);

                // 5. Sonucu İşleme
                if (result?.Candidates?.Count > 0 && result.Candidates[0].Content?.Parts?.Count > 0)
                {
                    model.YapayZekaYaniti = result.Candidates[0].Content.Parts[0].Text;
                }
                else
                {
                    model.YapayZekaYaniti = "Yapay zekadan yanıt alınamadı veya yanıt uygunsuz bulundu. Lütfen tekrar deneyin.";
                }
            }
            catch (Exception ex)
            {
                // Ağ hatası, serileştirme hatası vb. yakalar
                ModelState.AddModelError(string.Empty, $"API çağrısı sırasında beklenmedik bir hata oluştu: {ex.Message}");
                model.YapayZekaYaniti = null;
            }

            return View(model);
        }
    }
}