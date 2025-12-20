using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FitnessCenterProject.Models.Gemini
{
    public class Content
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty; // ✅ UYARI GİDERİLDİ

        [JsonPropertyName("parts")]
        public List<Part> Parts { get; set; } = new List<Part>();
    }

    public class Part
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty; // ✅ UYARI GİDERİLDİ
    }

    public class GeminiChatRequest
    {
        [JsonPropertyName("contents")]
        public List<Content> Contents { get; set; } = new List<Content>();
    }
}