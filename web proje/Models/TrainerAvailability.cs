using System;
using System.ComponentModel.DataAnnotations;

namespace FitnessCenterProject.Models
{
    public class TrainerAvailability
    {
        [Key]
        public int AvailabilityId { get; set; }

        public DayOfWeek DayOfWeek { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }

        // Foreign Key
        public int TrainerId { get; set; }

        // Navigation Property - FIXED: '?' eklendi
        public Trainer? Trainer { get; set; }
    }
}