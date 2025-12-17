using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using FitnessCenterProject.Models;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.DependencyInjection;

namespace FitnessCenterProject.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            // Veritabanı Migration'larını uygular (Program.cs'ten buraya taşıdık, daha derli toplu)
            context.Database.Migrate();

            // 1. ROLLERİ OLUŞTURMA
            string[] roleNames = { "Admin", "Member" };

            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // 2. ADMIN KULLANICISINI OLUŞTURMA ve ROLE ATAMA
            var adminUserEmail = "ogrencinumarasi@sakarya.edu.tr";
            var adminUser = await userManager.FindByEmailAsync(adminUserEmail);

            if (adminUser == null)
            {
                var newAdmin = new ApplicationUser
                {
                    UserName = adminUserEmail,
                    Email = adminUserEmail,
                    EmailConfirmed = true,
                    FullName = "Sistem Yöneticisi"
                };

                var result = await userManager.CreateAsync(newAdmin, "sau");

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(newAdmin, "Admin");
                }
            }

            // 3. EĞİTMEN VE HİZMET VERİLERİNİ EKLEME (Trainer ve Service)
            if (!context.Trainers.Any())
            {
                context.Trainers.AddRange(
                    new Trainer { Name = "Ahmet Taşdemir", Specialty = "Kardiyo Uzmanı" },
                    new Trainer { Name = "Muhammet Ali Çiftçi", Specialty = "Vücut Geliştirme" }
                );
            }

            if (!context.Services.Any())
            {
                context.Services.AddRange(
                    new Service { Name = "Personal Antrenman", DurationMinutes = 60, Price = 150 },
                    new Service { Name = "Diyetisyen Görüşmesi", DurationMinutes = 45, Price = 100 },
                    new Service { Name = "Boks", DurationMinutes = 60, Price = 120 }
                );
            }

            // Değişiklikleri kaydet
            await context.SaveChangesAsync();
        }
    }
}