using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FitnessCenterProject.Models
{
    public class Service
    {
        [Key]
        public int ServiceId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; } // string? ile null atanabilir yapıldı
        public decimal Price { get; set; }
        public int DurationMinutes { get; set; }

        // Gezinim Özellikleri - FIXED: Başlatıldı
        public ICollection<TrainerService> TrainerServices { get; set; } = new List<TrainerService>();
    }
}