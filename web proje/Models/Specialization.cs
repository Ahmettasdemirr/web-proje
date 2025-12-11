using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FitnessCenterProject.Models
{
    public class Specialization
    {
        [Key]
        public int SpecializationId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        // Gezinim Özellikleri - FIXED: Başlatıldı
        public ICollection<TrainerSpecialization> TrainerSpecializations { get; set; } = new List<TrainerSpecialization>();
    }
}