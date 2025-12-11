using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FitnessCenterProject.Models
{
    public class Appointment
    {
        [Key]
        public int AppointmentId { get; set; }

        [Required(ErrorMessage = "Başlangıç zamanı zorunludur.")]
        [Display(Name = "Başlangıç Zamanı")]
        public DateTime StartTime { get; set; }

        [Required(ErrorMessage = "Bitiş zamanı zorunludur.")]
        [Display(Name = "Bitiş Zamanı")]
        public DateTime EndTime { get; set; }

        public bool IsConfirmed { get; set; }
        public string? Notes { get; set; }

        // Foreign Keys (Yabancı Anahtarlar)
        public int TrainerId { get; set; }
        public int ServiceId { get; set; }
        public string UserId { get; set; } = string.Empty;

        // Navigation Properties (Gezinim Özellikleri)
        public Trainer? Trainer { get; set; }
        public Service? Service { get; set; }
        public ApplicationUser? User { get; set; }
    }
}