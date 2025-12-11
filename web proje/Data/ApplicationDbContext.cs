using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using FitnessCenterProject.Models; // Model sınıflarınızın namespace'i
using Microsoft.AspNetCore.Identity; // IdentityRole için gerekli olabilir
using System.Linq; // LINQ metotları (SelectMany, Where) için gerekli

namespace FitnessCenterProject.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Proje Varlıkları (DbSet'ler)
        public DbSet<Trainer> Trainers { get; set; } = default!;
        public DbSet<Service> Services { get; set; } = default!;
        public DbSet<Specialization> Specializations { get; set; } = default!;

        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<TrainerAvailability> TrainerAvailabilities { get; set; } = default!;

        // Ara Tablolar (Çoka-Çok İlişkileri)
        public DbSet<TrainerSpecialization> TrainerSpecializations { get; set; } = default!;
        public DbSet<TrainerService> TrainerServices { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // IdentityDbContext'in kendi ilişkilerini ve tablolarını oluşturması için
            base.OnModelCreating(modelBuilder);

            // PostgreSQL Zaman Dilimi Yönetimi için KRİTİK AYAR:
            // Tüm DateTime ve DateTime? alanlarını "timestamp without time zone" olarak eşleştirir.
            // Bu, 'column "StartTime" cannot be cast automatically' hatasını çözer.
            foreach (var property in modelBuilder.Model.GetEntityTypes()
                .SelectMany(t => t.GetProperties())
                .Where(p => p.ClrType == typeof(DateTime) || p.ClrType == typeof(DateTime?)))
            {
                property.SetColumnType("timestamp without time zone");
            }

            // --- EF Core'a Çoka-Çok İlişkileri için Bileşik Anahtarları tanıtıyoruz ---

            // Trainer - Specialization İlişkisi
            modelBuilder.Entity<TrainerSpecialization>()
                .HasKey(ts => new { ts.TrainerId, ts.SpecializationId });

            // Trainer - Service İlişkisi
            modelBuilder.Entity<TrainerService>()
                .HasKey(trs => new { trs.TrainerId, trs.ServiceId });
        }
    }
}