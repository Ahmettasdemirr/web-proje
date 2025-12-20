using FitnessCenterProject.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System; // IServiceProvider için gerekli

namespace FitnessCenterProject.Data
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            // KRİTİK DÜZELTME: UserManager servisini 'IdentityUser' yerine 'ApplicationUser' ile alın.
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            using (var context = new ApplicationDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>()))
            {
                // Veritabanının oluşturulduğundan emin ol
                context.Database.EnsureCreated();

                // Rolleri kontrol et ve oluştur (RoleManager doğru)
                if (!await roleManager.RoleExistsAsync("Admin"))
                {
                    await roleManager.CreateAsync(new IdentityRole("Admin"));
                }
                if (!await roleManager.RoleExistsAsync("User"))
                {
                    await roleManager.CreateAsync(new IdentityRole("User"));
                }

                // Admin Kullanıcısını kontrol et ve oluştur
                string adminEmail = "admin@fit.com";
                if (await userManager.FindByEmailAsync(adminEmail) == null)
                {
                    // KRİTİK DÜZELTME: Yeni kullanıcıyı IdentityUser yerine ApplicationUser olarak oluşturun.
                    var adminUser = new ApplicationUser
                    {
                        UserName = adminEmail,
                        Email = adminEmail,
                        EmailConfirmed = true
                    };
                    var result = await userManager.CreateAsync(adminUser, "Admin123!");
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(adminUser, "Admin");
                    }
                }

                // Normal Kullanıcıyı kontrol et ve oluştur
                string userEmail = "user@fit.com";
                if (await userManager.FindByEmailAsync(userEmail) == null)
                {
                    // KRİTİK DÜZELTME: Yeni kullanıcıyı IdentityUser yerine ApplicationUser olarak oluşturun.
                    var normalUser = new ApplicationUser
                    {
                        UserName = userEmail,
                        Email = userEmail,
                        EmailConfirmed = true
                    };
                    var result = await userManager.CreateAsync(normalUser, "User123!");
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(normalUser, "User");
                    }
                }

                // --- Hizmet Verilerini Ekle ---
                if (!context.Services.Any())
                {
                    var services = new Service[]
                    {
                        new Service { Name = "Personal Antrenman", Description = "Kişiye özel 1'e 1 antrenman programı.", DurationMinutes = 60, Price = 150 },
                        new Service { Name = "Grup Egzersizleri", Description = "10 kişiye kadar küçük grup dersleri.", DurationMinutes = 45, Price = 80 },
                        new Service { Name = "Pilates", Description = "Mat veya reformer pilates dersleri.", DurationMinutes = 60, Price = 120 },
                        new Service { Name = "Boks", Description = "Teknik ve kardiyo odaklı boks dersleri.", DurationMinutes = 60, Price = 160 },
                        new Service { Name = "Diyetisyen Görüşmesi", Description = "Kişiye özel beslenme danışmanlığı.", DurationMinutes = 30, Price = 200 }
                    };
                    await context.Services.AddRangeAsync(services);
                    await context.SaveChangesAsync();
                }

                var serviceList = await context.Services.ToListAsync();
                var personalTrainingId = serviceList.FirstOrDefault(s => s.Name == "Personal Antrenman")?.ServiceId ?? 1;
                var groupExerciseId = serviceList.FirstOrDefault(s => s.Name == "Grup Egzersizleri")?.ServiceId ?? 2;
                var pilatesId = serviceList.FirstOrDefault(s => s.Name == "Pilates")?.ServiceId ?? 3;
                var boxingId = serviceList.FirstOrDefault(s => s.Name == "Boks")?.ServiceId ?? 4;


                // --- Eğitmen Verilerini Ekle ---
                if (!context.Trainers.Any())
                {
                    var trainers = new Trainer[]
                    {
                        new Trainer { Name = "Ahmet Taşdemir" },
                        new Trainer { Name = "Muhammet Yıldız" },
                        new Trainer { Name = "Ayşe Kaya" }
                    };
                    await context.Trainers.AddRangeAsync(trainers);
                    await context.SaveChangesAsync();
                }

                var ahmetId = context.Trainers.FirstOrDefault(t => t.Name == "Ahmet Taşdemir")?.TrainerId;
                var muhammetId = context.Trainers.FirstOrDefault(t => t.Name == "Muhammet Yıldız")?.TrainerId;
                var ayseId = context.Trainers.FirstOrDefault(t => t.Name == "Ayşe Kaya")?.TrainerId;


                // --- Eğitmen-Hizmet İlişkilerini Ekle (TrainerService) ---
                if (!context.TrainerServices.Any())
                {
                    var trainerServices = new List<TrainerService>();

                    if (ahmetId.HasValue)
                    {
                        trainerServices.Add(new TrainerService { TrainerId = ahmetId.Value, ServiceId = personalTrainingId });
                        trainerServices.Add(new TrainerService { TrainerId = ahmetId.Value, ServiceId = boxingId });
                    }
                    if (muhammetId.HasValue)
                    {
                        trainerServices.Add(new TrainerService { TrainerId = muhammetId.Value, ServiceId = personalTrainingId });
                        trainerServices.Add(new TrainerService { TrainerId = muhammetId.Value, ServiceId = groupExerciseId });
                    }
                    if (ayseId.HasValue)
                    {
                        trainerServices.Add(new TrainerService { TrainerId = ayseId.Value, ServiceId = pilatesId });
                        trainerServices.Add(new TrainerService { TrainerId = ayseId.Value, ServiceId = groupExerciseId });
                    }

                    await context.TrainerServices.AddRangeAsync(trainerServices);
                    await context.SaveChangesAsync();
                }
            }
        }
    }
}