using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using FitnessCenterProject.Data;
using FitnessCenterProject.Models;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;

var builder = WebApplication.CreateBuilder(args);

// --- 1. Veritabanı Bağlantısı ve DbContext Servisi ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                             ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// PostgreSQL DbContext Servisini Kaydet
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// --- 2. Identity ve Rol Desteği Servisleri ---
// Custom ApplicationUser kullanılıyor
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    // Şifre gereksinimleri (Opsiyonel olarak basitleştirildi)
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 3;
    options.SignIn.RequireConfirmedAccount = false;
})
    // Rol Desteği Eklendi
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// Yetkilendirme başarısız olursa Login sayfasının yolunu belirtir.
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = $"/Identity/Account/Login";
});

// GEREKLİ EKLEME: HttpClient servisini dependency injection'a ekler.
builder.Services.AddHttpClient();


// --- 3. MVC (Controller ve View) Servisleri ---
// Tüm Controller'lar için varsayılan yetkilendirmeyi kullanıyoruz.
builder.Services.AddControllersWithViews();


// --- 4. Razor Sayfaları Servisi (Identity UI için gerekli) ---
builder.Services.AddRazorPages();


var app = builder.Build();

// --- 5. Seed Data ve Otomatik Migration Uygulama ---
using (var scope = app.Services.CreateScope())
{
    var serviceProvider = scope.ServiceProvider;
    try
    {
        // KRİTİK DÜZELTME: SeedData metod adı "Initialize" olmalı. 
        // .Wait() ile asenkron işlem senkronize çalıştırılır.
        FitnessCenterProject.Data.SeedData.Initialize(serviceProvider).Wait();
    }
    catch (Exception ex)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Veritabanı seed işlemi sırasında bir hata oluştu.");
    }
}


// --- 6. HTTP İstek İşlem Hattı (Pipeline) ---
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();


app.UseAuthentication();
app.UseAuthorization();

// --- 7. Endpoint Eşleştirme (Controller ve Razor Sayfaları için) ---
app.MapRazorPages();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();