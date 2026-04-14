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
    [Authorize]
    public class AdminController(
        ApplicationDbContext context,
        IRecurringJobManager recurringJobManager,
        MarketDataService marketDataService) : Controller
    {
        private const string SuccessMessageKey = "SuccessMessage";
        private const string ErrorMessageKey = "ErrorMessage";
        private const string DefaultAdminUser = "admin";
        private const string DefaultAdminPass = "altin2026";

        // Sabitler (Magic String çözümü)
        private const string MenuListAction = "MenuList";
        private const string SettingsAction = "Settings";
        private const string SponsorListAction = "SponsorList"; 

        // --- GİRİŞ YAPMA SAYFASI ---
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity is { IsAuthenticated: true })
                return RedirectToAction(SettingsAction);

            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Error = "Lütfen alanları eksiksiz doldurun.";
                return View();
            }

            var settings = context.SiteSettings.FirstOrDefault();

            string dbUser = settings?.AdminUsername ?? DefaultAdminUser;
            string dbPass = settings?.AdminPassword ?? DefaultAdminPass;

            if (username == dbUser && password == dbPass)
            {
                List<Claim> claims = [new Claim(ClaimTypes.Name, username)];
                ClaimsIdentity identity = new(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                ClaimsPrincipal principal = new(identity);

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                return RedirectToAction(SettingsAction);
            }

            ViewBag.Error = "Kullanıcı adı veya şifre hatalı kral!";
            return View();
        }

        // --- MANUEL VERİ ÇEKME TETİKLEYİCİSİ ---
        [HttpPost]
        public async Task<IActionResult> TriggerManualFetch()
        {
            try
            {
                await marketDataService.FetchAndSaveMarketDataAsync();
                TempData[SuccessMessageKey] = "Manuel veri çekme işlemi tetiklendi! Veritabanına o anki fiyatlar başarıyla eklendi.";
            }
            catch (Exception ex)
            {
                TempData[ErrorMessageKey] = $"Veri çekilirken hata oluştu: {ex.Message}";
            }

            return RedirectToAction(SettingsAction);
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

        [HttpGet]
        public IActionResult Settings()
        {
            var setting = context.SiteSettings.FirstOrDefault();
            if (setting == null)
            {
                setting = new SiteSetting
                {
                    ApiFetchIntervalInHours = 2,
                    DataRetentionDays = 5,
                    MetaTitle = "Altın Zamanı - Canlı Altın ve Döviz",
                    MetaDescription = "Anlık altın fiyatları ve döviz kurları.",
                    AdminUsername = DefaultAdminUser,
                    AdminPassword = DefaultAdminPass
                };
                context.SiteSettings.Add(setting);
                context.SaveChanges();
            }

            return View(setting);
        }

        [HttpPost]
        public IActionResult Settings(SiteSetting model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var setting = context.SiteSettings.FirstOrDefault();
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
                setting.IsInstagramActive = model.IsInstagramActive;

                setting.TwitterUrl = model.TwitterUrl;
                setting.IsTwitterActive = model.IsTwitterActive;

                setting.FacebookUrl = model.FacebookUrl;
                setting.IsFacebookActive = model.IsFacebookActive;

                // 5. Admin Bilgileri
                setting.AdminUsername = model.AdminUsername;
                setting.AdminPassword = model.AdminPassword;

                context.SaveChanges();

                string apiCron = model.ApiFetchIntervalInHours >= 24
                    ? "0 0 * * *"
                    : $"0 */{model.ApiFetchIntervalInHours} * * *";

                recurringJobManager.AddOrUpdate<MarketDataService>(
                    "api-veri-cekme-gorevi",
                    service => service.FetchAndSaveMarketDataAsync(),
                    apiCron
                );

                recurringJobManager.AddOrUpdate<MarketDataService>(
                    "eski-verileri-temizleme-gorevi",
                    service => service.CleanUpOldDataAsync(),
                    "0 1 * * *"
                );

                TempData[SuccessMessageKey] = "Başarılı!";
            }

            return View(setting);
        }

        // ==========================================
        //         MENÜ YÖNETİMİ (CRUD İŞLEMLERİ)
        // ==========================================

        public IActionResult MenuList()
        {
            var menus = context.Menus.OrderBy(m => m.Order).ToList();
            return View(menus);
        }

        public IActionResult CreateMenu()
        {
            return View(new Menu { IsActive = true });
        }

        [HttpPost]
        public IActionResult CreateMenu(Menu model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            context.Menus.Add(model);
            context.SaveChanges();
            TempData[SuccessMessageKey] = "Menü başarıyla eklendi.";
            return RedirectToAction(MenuListAction);
        }

        [HttpGet]
        public IActionResult EditMenu(int id)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction(MenuListAction);
            }

            var menu = context.Menus.Find(id);
            if (menu == null) return NotFound();

            return View(menu);
        }

        [HttpPost]
        public IActionResult EditMenu(Menu model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            context.Menus.Update(model);
            context.SaveChanges();
            TempData[SuccessMessageKey] = "Menü güncellendi.";
            return RedirectToAction(MenuListAction);
        }

        [HttpPost]
        public IActionResult DeleteMenu(int id)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction(MenuListAction);
            }

            var menu = context.Menus.Find(id);
            if (menu != null)
            {
                context.Menus.Remove(menu);
                context.SaveChanges();
            }
            return RedirectToAction(MenuListAction);
        }

        // ==========================================
        //         SPONSOR YÖNETİMİ (CRUD İŞLEMLERİ)
        // ==========================================

        public IActionResult SponsorList()
        {
            var sponsors = context.Sponsors.OrderBy(s => s.Order).ToList();
            return View(sponsors);
        }

        [HttpGet]
        public IActionResult CreateSponsor()
        {
            return View(new Sponsor { IsActive = true });
        }

        [HttpPost]
        public IActionResult CreateSponsor(Sponsor model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            context.Sponsors.Add(model);
            context.SaveChanges();
            TempData[SuccessMessageKey] = "Sponsor başarıyla eklendi.";
            return RedirectToAction(SponsorListAction);
        }

        [HttpGet]
        public IActionResult EditSponsor(int id)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction(SponsorListAction);
            }

            var sponsor = context.Sponsors.Find(id);
            if (sponsor == null) return NotFound();

            return View(sponsor);
        }

        [HttpPost]
        public IActionResult EditSponsor(Sponsor model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            context.Sponsors.Update(model);
            context.SaveChanges();
            TempData[SuccessMessageKey] = "Sponsor güncellendi.";
            return RedirectToAction(SponsorListAction);
        }

        [HttpPost]
        public IActionResult DeleteSponsor(int id)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction(SponsorListAction);
            }

            var sponsor = context.Sponsors.Find(id);
            if (sponsor != null)
            {
                context.Sponsors.Remove(sponsor);
                context.SaveChanges();
            }
            return RedirectToAction(SponsorListAction);
        }
    }
}