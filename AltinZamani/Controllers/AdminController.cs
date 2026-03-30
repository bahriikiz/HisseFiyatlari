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
                // 1. Veritabanındaki ayarları senin formdan gönderdiklerinle güncelle
                setting.ApiFetchIntervalInHours = model.ApiFetchIntervalInHours;
                setting.DataRetentionDays = model.DataRetentionDays;
                setting.CleanupIntervalInHours = model.CleanupIntervalInHours;
                _context.SaveChanges();

                // 2. API VERİ ÇEKME TAKVİMİNİ GÜNCELLE
                string apiCron = $"0 */{model.ApiFetchIntervalInHours} * * *";
                _recurringJobManager.AddOrUpdate<MarketDataService>(
                    "api-veri-cekme-gorevi",
                    service => service.FetchAndSaveMarketDataAsync(),
                    apiCron
                );

                // 3. ESKİ VERİ TEMİZLEME TAKVİMİNİ GÜNCELLE 
                string cleanupCron = $"0 */{model.CleanupIntervalInHours} * * *";
                _recurringJobManager.AddOrUpdate<MarketDataService>(
                    "eski-verileri-temizleme-gorevi",
                    service => service.CleanUpOldDataAsync(),
                    cleanupCron
                );

                TempData["SuccessMessage"] = "Tüm sistem ayarları başarıyla kaydedildi ve Hangfire otomasyon takvimleri güncellendi!";
            }

            return View(setting);
        }
    }
}