using AltinZamani.Data;
using AltinZamani.Models;
using AltinZamani.Services;
using Hangfire;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AltinZamani.Controllers
{
    // YENİ: Bu sınıfa girmek için artık kimlik şart!
    [Authorize]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IRecurringJobManager _recurringJobManager;

        public AdminController(ApplicationDbContext context, IRecurringJobManager recurringJobManager)
        {
            _context = context;
            _recurringJobManager = recurringJobManager;
        }

        // --- GİRİŞ YAPMA SAYFASI (Buraya herkes girebilir) ---
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login()
        {
            // Eğer zaten giriş yapmışsa direkt panele yönlendir
            if (User.Identity != null && User.Identity.IsAuthenticated)
                return RedirectToAction("Settings");

            return View();
        }

        // --- GİRİŞ YAPMA İŞLEMİ ---
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            // Veritabanından güncel şifreyi çekiyoruz 
            var settings = _context.SiteSettings.FirstOrDefault();
            string dbUser = settings?.AdminUsername;
            string dbPass = settings?.AdminPassword;

            // Artık veritabanındaki bilgilerle eşleşiyorsa giriş yapacak!
            if (username == dbUser && password == dbPass)
            {
                var claims = new List<Claim> { new Claim(ClaimTypes.Name, username) };
                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                return RedirectToAction("Settings");
            }

            ViewBag.Error = "Kullanıcı adı veya şifre hatalı kral!";
            return View();
        }

        // --- ÇIKIŞ YAPMA İŞLEMİ ---
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }


        // ==========================================
        //         AYARLAR (SETTINGS) KISMI
        // ==========================================

        // GET: Ayarlar Sayfasını Ekrana Getirir
        public IActionResult Settings()
        {
            var setting = _context.SiteSettings.FirstOrDefault();
            if (setting == null)
            {
                // Varsayılan ilk ayarlar
                setting = new SiteSetting
                {
                    ApiFetchIntervalInHours = 2,
                    DataRetentionDays = 5,
                    MetaTitle = "Altın Zamanı - Canlı Altın ve Döviz",
                    MetaDescription = "Anlık altın fiyatları ve döviz kurları."
                };
                _context.SiteSettings.Add(setting);
                _context.SaveChanges();
            }

            return View(setting);
        }

        // POST: Ayarları Kaydeder ve Hangfire'ı Günceller
        [HttpPost]
        public IActionResult Settings(SiteSetting model)
        {
            var setting = _context.SiteSettings.FirstOrDefault();
            if (setting != null)
            {
                // 1. OTOMASYON AYARLARI
                setting.ApiFetchIntervalInHours = model.ApiFetchIntervalInHours;
                setting.DataRetentionDays = model.DataRetentionDays;
                setting.BannerAdCode = model.BannerAdCode;

                // 2. REKLAM VE ANALİZ AYARLARI
                setting.AdsenseCode = model.AdsenseCode;
                setting.AnalyticsCode = model.AnalyticsCode;
                setting.TopAdCode = model.TopAdCode;
                setting.LeftAdCode = model.LeftAdCode;
                setting.RightAdCode = model.RightAdCode;

                // 3. SEO AYARLARI
                setting.MetaTitle = model.MetaTitle;
                setting.MetaDescription = model.MetaDescription;
                setting.MetaKeywords = model.MetaKeywords;

                // 4. İLETİŞİM VE SOSYAL MEDYA
                setting.ContactEmail = model.ContactEmail;
                setting.FooterText = model.FooterText;
                setting.InstagramUrl = model.InstagramUrl;
                setting.TwitterUrl = model.TwitterUrl;
                setting.FacebookUrl = model.FacebookUrl;

                // 5.Admin Bilgileri
                setting.AdminUsername = model.AdminUsername;
                setting.AdminPassword = model.AdminPassword;

                _context.SaveChanges();

                // API Çekme Görevi (Hata Korumalı)
                string apiCron = model.ApiFetchIntervalInHours >= 24
                    ? "0 0 * * *"
                    : $"0 */{model.ApiFetchIntervalInHours} * * *";

                _recurringJobManager.AddOrUpdate<MarketDataService>(
                    "api-veri-cekme-gorevi",
                    service => service.FetchAndSaveMarketDataAsync(),
                    apiCron
                );

                // Temizlik Görevi (Sabit Her Gece 01:00)
                _recurringJobManager.AddOrUpdate<MarketDataService>(
                    "eski-verileri-temizleme-gorevi",
                    service => service.CleanUpOldDataAsync(),
                    "0 1 * * *"
                );

                TempData["SuccessMessage"] = "Başarılı!";
            }

            return View(setting);
        }
    }
}