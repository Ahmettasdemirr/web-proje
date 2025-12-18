using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic; // List<T> kullanıldığı için gerekli olabilir
using FitnessCenterProject.Models;

public class ReportController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;

    // HttpClient'ı Dependency Injection ile alıyoruz
    public ReportController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    // GET: /Report/AvailableTrainers
    public async Task<IActionResult> AvailableTrainers(string date, string startTime, int duration = 60)
    {
        // 1. URL Hazırlığı (Lütfen port numarasını kendi projenizin çalıştığı port ile değiştirin)
        
        var baseUrl = "https://localhost:7891";

        // Örnek varsayılan tarih: Yarın saat 10:00
        if (string.IsNullOrEmpty(date) || string.IsNullOrEmpty(startTime))
        {
            date = System.DateTime.Now.AddDays(1).ToString("yyyy-MM-dd");
            startTime = "10:00";
        }

        string apiUrl = $"{baseUrl}/api/TrainersApi/available?date={date}&startTime={startTime}&duration={duration}";

        // 2. API Çağrısı
        var httpClient = _httpClientFactory.CreateClient();

        // Hata durumunda HTTPS sertifikası sorunları yaşarsanız, bu satırı geçici olarak kullanabilirsiniz:
        // var handler = new HttpClientHandler { ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true };
        // var httpClient = new HttpClient(handler);


        var response = await httpClient.GetAsync(apiUrl);

        if (!response.IsSuccessStatusCode)
        {
            ViewBag.ErrorMessage = "API'ye erişilemedi veya belirtilen saatte müsait eğitmen bulunamadı.";
            // Hata durumunda boş liste döndürebiliriz
            return View(new List<TrainerApiResult>());
        }

        // 3. JSON verisini okuma ve C# nesnesine dönüştürme
        var jsonResponse = await response.Content.ReadAsStringAsync();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var trainers = JsonSerializer.Deserialize<List<TrainerApiResult>>(jsonResponse, options);

        ViewBag.Date = date;
        ViewBag.StartTime = startTime;
        ViewBag.Duration = duration;
        ViewBag.ApiUrl = apiUrl; // Kontrol amaçlı View'de göstermek için

        return View(trainers);
    }
}