using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace FitnessCenterProject.Models
{
    public class ApplicationUser : IdentityUser
    {
        // Ek bilgi olarak tam adı tutabiliriz
        public string? FullName { get; set; }

        // Yapay Zeka gereksinimi için vücut bilgileri
        public double? HeightCm { get; set; } // Boy (cm)
        public double? WeightKg { get; set; } // Kilo (kg)

        // Gezinim özelliği (Navigation Property) - FIXED: Başlatıldı
        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    }
}