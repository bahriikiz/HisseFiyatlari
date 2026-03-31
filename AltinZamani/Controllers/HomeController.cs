using AltinZamani.Data;
using AltinZamani.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace AltinZamani.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index(string currency = "TRY")
        {
            // Son 24 saat içindeki en güncel verileri alıyoruz (Today yerine 24 saat daha güvenlidir)
            var son24Saat = DateTime.Now.AddHours(-24);

            var marketData = _context.MarketDatas
                .Where(m => m.SiteType == "altinzamani" && m.RecordDate >= son24Saat)
                .AsEnumerable() // Gruplama işlemini bellek üzerinde yaparak performans ve uyumluluk sağlıyoruz
                .GroupBy(m => m.Name)
                .Select(g => g.OrderByDescending(m => m.RecordDate).First())
                .ToList();

            decimal bolenDeger = 1;
            string sembol = "TL"; // ₺ sembolü yerine soru işareti hatasını önlemek için TL yazdık

            if (currency != "TRY")
            {
                var secilenDoviz = marketData.FirstOrDefault(m => m.Name.Equals(currency, StringComparison.OrdinalIgnoreCase));

                if (secilenDoviz != null && secilenDoviz.LastPrice > 0)
                {
                    bolenDeger = secilenDoviz.LastPrice;
                    sembol = currency switch
                    {
                        "USD" => "$",
                        "EUR" => "€",
                        "GBP" => "£",
                        _ => currency
                    };
                }
                else
                {
                    currency = "TRY";
                }
            }

            // Fiyat dönüştürme işlemi
            if (bolenDeger != 1)
            {
                foreach (var item in marketData)
                {
                    item.LastPrice = item.LastPrice / bolenDeger;
                }
            }

            ViewBag.SelectedCurrency = currency;
            ViewBag.CurrencySymbol = sembol;

            return View(marketData);
        }

        // --- GRAFİK İÇİN YENİ EKLENEN METOT ---
        [HttpGet]
        public IActionResult GetChartData(string name = "gram-altin")
        {
            // Son 24 saatlik fiyat geçmişini getirir
            var limit = DateTime.Now.AddHours(-24);

            var history = _context.MarketDatas
                .Where(m => m.Name == name && m.RecordDate >= limit)
                .OrderBy(m => m.RecordDate)
                .Select(m => new {
                    price = m.LastPrice,
                    time = m.RecordDate.ToString("HH:mm")
                })
                .ToList();

            return Json(history);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    
    [HttpGet]
        public IActionResult SetLanguage(string culture, string returnUrl = "/")
        {
            // Kullanıcının seçtiği dili tarayıcıya 1 yıllığına kaydediyoruz
            Response.Cookies.Append("SiteLanguage", culture, new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1)
            });

            // Kullanıcıyı geldiği sayfaya geri gönderiyoruz
            return LocalRedirect(returnUrl);
        }
    }
}