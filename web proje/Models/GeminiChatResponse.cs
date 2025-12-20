using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FitnessCenterProject.Models.Gemini
{
    public class GeminiChatResponse
    {
        [JsonPropertyName("candidates")]
        public List<Candidate> Candidates { get; set; } = new List<Candidate>();
    }

    public class Candidate
    {
        [JsonPropertyName("content")]
        // ✅ UYARI GİDERİLDİ: Artık null olabilir veya yeni Content atanabilir.
        public Content? Content { get; set; }
    }
    // Content ve Part sınıfları GeminiChatRequest.cs içinde tanımlıdır
}