using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FitnessCenterProject.Models
{
    public class Trainer
    {
        [Key]
        public int TrainerId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        // YENİ EKLENEN ÖZELLİK: Specialty (Uzmanlık alanı)
        [StringLength(100)]
        public string Specialty { get; set; } = string.Empty;

        // Gezinim Özellikleri (Diğer tablolara bağlantılar)
        public ICollection<TrainerSpecialization> TrainerSpecializations { get; set; } = new List<TrainerSpecialization>();
        public ICollection<TrainerService> TrainerServices { get; set; } = new List<TrainerService>();
        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
        public ICollection<TrainerAvailability> Availabilities { get; set; } = new List<TrainerAvailability>();
    }
}