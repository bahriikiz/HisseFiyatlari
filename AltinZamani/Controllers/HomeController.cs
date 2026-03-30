using AltinZamani.Data;
using AltinZamani.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace AltinZamani.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        // Veritabaný baðlantýmýzý (ApplicationDbContext) Controller'a enjekte ediyoruz
        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index(string currency = "TRY")
        {
            var bugun = DateTime.Today;

            // 1. Tüm güncel verileri çekiyoruz (Hepsi TL bazýnda)
            var marketData = _context.MarketDatas
                .Where(m => m.SiteType == "altinzamani" && m.RecordDate.Date == bugun)
                .GroupBy(m => m.Name)
                .Select(g => g.OrderByDescending(m => m.RecordDate).First())
                .ToList();

            decimal bolenDeger = 1;
            string sembol = "?";

            // 2. Eðer kullanýcý TL dýþýnda bir kur seçtiyse hesaplama yapýyoruz
            if (currency != "TRY")
            {
                var secilenDoviz = marketData.FirstOrDefault(m => m.Name == currency);

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
                    currency = "TRY"; // Eðer kur bulunamazsa güvenliðe alýp TL'ye dön
                }
            }

            // 3. Fiyatlarý seçilen kura göre bölüþtürüyoruz
            if (bolenDeger != 1)
            {
                foreach (var item in marketData)
                {
                    // Temel dövizleri (USD seçiliyken USD'yi vs.) 1'e eþitlememek için ufak bir kontrol eklenebilir
                    // Ama genel mantýkta her þey o kura bölünür.
                    item.LastPrice = item.LastPrice / bolenDeger;
                }
            }

            // Seçilen kuru ve sembolü arayüze (View) gönderiyoruz ki butonlarý boyayabilelim
            ViewBag.SelectedCurrency = currency;
            ViewBag.CurrencySymbol = sembol;

            return View(marketData);
        }
    }
}