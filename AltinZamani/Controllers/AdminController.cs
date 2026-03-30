using AltinZamani.Data;
using AltinZamani.Models;
using AltinZamani.Services;
using Hangfire;
using Microsoft.AspNetCore.Mvc;

namespace AltinZamani.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IRecurringJobManager _recurringJobManager;

        // Veritabanı ve Hangfire yöneticisini içeri alıyoruz
        public AdminController(ApplicationDbContext context, IRecurringJobManager recurringJobManager)
        {
            _context = context;
            _recurringJobManager = recurringJobManager;
        }

        // GET: Ayarlar Sayfasını Ekrana Getirir
        public IActionResult Settings()
        {
            // Veritabanından ilk ayar kaydını al, yoksa boş bir tane oluştur
            var setting = _context.SiteSettings.FirstOrDefault();
            if (setting == null)
            {
                setting = new SiteSetting { ApiFetchIntervalInHours = 2, DataRetentionDays = 5 };
                _context.SiteSettings.Add(setting);
                _context.SaveChanges();
            }

            return View(setting);
        }

        // POST: Sen "Kaydet" butonuna bastığında çalışır
        [HttpPost]
        public IActionResult Settings(SiteSetting model)
        {
            var setting = _context.SiteSettings.FirstOrDefault();
            if (setting != null)
            {
                // Sadece gerekli olanları güncelliyoruz
                setting.ApiFetchIntervalInHours = model.ApiFetchIntervalInHours;
                setting.DataRetentionDays = model.DataRetentionDays;

                // Diğer SEO ve İletişim alanlarını da burada güncellediğinden emin ol (Örn: setting.MetaTitle = model.MetaTitle vb.)

                _context.SaveChanges();

                // 1. API VERİ ÇEKME GÖREVİ (Hata Korumalı)
                string apiCron = model.ApiFetchIntervalInHours >= 24
                    ? "0 0 * * *"
                    : $"0 */{model.ApiFetchIntervalInHours} * * *";

                _recurringJobManager.AddOrUpdate<MarketDataService>(
                    "api-veri-cekme-gorevi",
                    service => service.FetchAndSaveMarketDataAsync(),
                    apiCron
                );

                // 2. TEMİZLİK GÖREVİ (Sabit her gece 01:00)
                _recurringJobManager.AddOrUpdate<MarketDataService>(
                    "eski-verileri-temizleme-gorevi",
                    service => service.CleanUpOldDataAsync(),
                    "0 1 * * *"
                );

                TempData["SuccessMessage"] = "Ayarlar kaydedildi.";
            }

            return View(setting);
        }
    }
}