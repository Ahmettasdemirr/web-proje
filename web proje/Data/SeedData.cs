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
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            // 1. Rolleri Oluşturma (Admin ve Üye rolleri)
            string[] roleNames = { "Admin", "Member" };

            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    // Rol veritabanında yoksa oluştur
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // 2. Admin Kullanıcısını Oluşturma ve Role Atama
            // Gereksinim: ogrencinumarasi@sakarya.edu.tr / Şifre: sau
            var adminUserEmail = "ogrencinumarasi@sakarya.edu.tr";
            var adminUser = await userManager.FindByEmailAsync(adminUserEmail);

            if (adminUser == null)
            {
                // Admin kullanıcısı yoksa oluştur
                var newAdmin = new ApplicationUser
                {
                    UserName = adminUserEmail,
                    Email = adminUserEmail,
                    EmailConfirmed = true,
                    FullName = "Sistem Yöneticisi"
                };

                // Şifre: "sau" ile kullanıcıyı oluştur
                var result = await userManager.CreateAsync(newAdmin, "sau");

                if (result.Succeeded)
                {
                    // Kullanıcı başarıyla oluşturulduysa Admin rolünü ata
                    await userManager.AddToRoleAsync(newAdmin, "Admin");
                }
            }
        }
    }
}