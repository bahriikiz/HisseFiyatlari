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

        public IActionResult Index()
        {
            var bugun = DateTime.Today;

            // Bugünün 'altinzamani' verilerini veritabanýndan çekiyoruz
            var marketData = _context.MarketDatas
                .Where(m => m.SiteType == "altinzamani" && m.RecordDate.Date == bugun)
                // Eðer ayný gün içinde birden fazla veri varsa, en son çekileni baz almasý için grupluyoruz
                .GroupBy(m => m.Name)
                .Select(g => g.OrderByDescending(m => m.RecordDate).First())
                .ToList();

            // Verileri Index.cshtml sayfasýna gönderiyoruz
            return View(marketData);
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
    }
}