using System.ComponentModel.DataAnnotations;

namespace FitnessCenterProject.Models
{
    public class AIRequestViewModel
    {
        [Required(ErrorMessage = "Lütfen boyunuzu giriniz.")]
        [Range(100, 250, ErrorMessage = "Boyunuz 100 ile 250 cm arasında olmalıdır.")]
        public int Boy { get; set; }

        [Required(ErrorMessage = "Lütfen kilonuzu giriniz.")]
        [Range(30, 300, ErrorMessage = "Kilonuz 30 ile 300 kg arasında olmalıdır.")]
        public int Kilo { get; set; }

        [Required(ErrorMessage = "Lütfen hedefinizi belirtiniz.")]
        public string Hedef { get; set; } // Örn: Kilo Verme, Kas Geliştirme, Dayanıklılık

        [Required(ErrorMessage = "Lütfen haftalık egzersiz sıklığınızı belirtiniz.")]
        public int HaftalikEgzersizSayisi { get; set; }

        public string? EkBilgiler { get; set; } // Örn: Alerjiler, özel tıbbi durumlar

        // Yanıtın saklanacağı alan
        public string? YapayZekaYaniti { get; set; }
    }
}