using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using FitnessCenterProject.Models;

public class ReportController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;

    public ReportController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    // GET: /Report/AvailableTrainers
    // KRİTİK DÜZELTME: serviceId varsayılan değeri 1'den 3'e çekildi.
    public async Task<IActionResult> AvailableTrainers(string date, string startTime, int duration = 60, int serviceId = 3)
    {
        // 1. URL Hazırlığı

        // !!! ÖNEMLİ: BU PORT NUMARASINI PROJENİZİN GÜNCEL ÇALIŞTIĞI HTTPS PORTU İLE DEĞİŞTİRİN !!!
        var baseUrl = "https://localhost:7891";

        if (string.IsNullOrEmpty(date) || string.IsNullOrEmpty(startTime))
        {
            date = System.DateTime.Now.AddDays(1).ToString("yyyy-MM-dd");
            startTime = "10:00";
        }

        string apiUrl = $"{baseUrl}/api/TrainersApi/available?date={date}&startTime={startTime}&duration={duration}&serviceId={serviceId}";

        // 2. API Çağrısı
        var httpClient = _httpClientFactory.CreateClient();
        var response = await httpClient.GetAsync(apiUrl);

        if (!response.IsSuccessStatusCode)
        {
            // Hata Durumu Yönetimi: Hata kodunu ve detayını yakala
            ViewBag.ErrorMessage = $"API'den beklenmeyen bir durum kodu alındı: {response.StatusCode}.";
            try
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                ViewBag.ErrorDetails = errorContent.Length > 1000 ? errorContent.Substring(0, 1000) + "..." : errorContent;
            }
            catch
            {
                ViewBag.ErrorDetails = "Detaylı hata mesajı alınamadı.";
            }

            // ViewBag'leri View'a gönderme
            ViewBag.Date = date;
            ViewBag.StartTime = startTime;
            ViewBag.Duration = duration;
            ViewBag.ApiUrl = apiUrl;
            ViewBag.ServiceId = serviceId;

            return View(new List<TrainerApiResult>());
        }

        // 3. JSON verisini okuma ve C# nesnesine dönüştürme (Başarılı Durum)
        var jsonResponse = await response.Content.ReadAsStringAsync();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        var trainers = JsonSerializer.Deserialize<List<TrainerApiResult>>(jsonResponse, options) ?? new List<TrainerApiResult>();

        ViewBag.Date = date;
        ViewBag.StartTime = startTime;
        ViewBag.Duration = duration;
        ViewBag.ApiUrl = apiUrl;
        ViewBag.ServiceId = serviceId;

        return View(trainers);
    }
}