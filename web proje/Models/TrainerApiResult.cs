namespace FitnessCenterProject.Models
{
    public class TrainerApiResult
    {
        public int TrainerId { get; set; }
        public string? Name { get; set; } // Null atanabilir yapıldı
        public string? Specialty { get; set; } // Null atanabilir yapıldı
        public string? Availability { get; set; } // 💡 Null atanabilir yapıldı (CS8602 uyarısını gidermek için)
    }
}