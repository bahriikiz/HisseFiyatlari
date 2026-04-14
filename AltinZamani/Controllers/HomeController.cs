using AltinZamani.Data;
using AltinZamani.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace AltinZamani.Controllers
{
    public class HomeController(ApplicationDbContext context) : Controller
    {
        public IActionResult Index(string currency = "TRY")
        {
            // Son 5 takvim gününü (Bugün dahil) garantiye almak için bir liste oluşturuyoruz
            var son5GunListesi = Enumerable.Range(0, 5)
                                           .Select(i => DateTime.Now.Date.AddDays(-i))
                                           .ToList();

            var baslangicTarihi = son5GunListesi.Last();

            // Veritabanından verileri çekiyoruz
            var rawData = context.MarketDatas
                .Where(m => m.SiteType == "altinzamani" && m.RecordDate >= baslangicTarihi)
                .ToList();

            // Dictionary'i her zaman 5 gün olacak şekilde başlatıyoruz
            var groupedData = new Dictionary<DateTime, List<MarketData>>();

            foreach (var gun in son5GunListesi)
            {
                var oGununVerileri = rawData
                    .Where(m => m.RecordDate.Date == gun)
                    .GroupBy(m => m.Name)
                    .Select(g => g.OrderByDescending(x => x.RecordDate).First())
                    .OrderByDescending(x => x.LastPrice)
                    .ToList();

                groupedData.Add(gun, oGununVerileri);
            }

            // Döviz hesaplama kısımları (Aynı kalıyor)
            decimal bolenDeger = 1;
            string sembol = "₺";
            if (currency != "TRY")
            {
                var bugunData = groupedData[DateTime.Now.Date];
                var secilenDoviz = bugunData?.FirstOrDefault(m => string.Equals(m.Name, currency, StringComparison.OrdinalIgnoreCase));
                if (secilenDoviz != null && secilenDoviz.LastPrice > 0)
                {
                    bolenDeger = secilenDoviz.LastPrice;
                    sembol = currency switch { "USD" => "$", "EUR" => "€", "GBP" => "£", _ => currency };
                }
            }

            if (bolenDeger != 1)
            {
                foreach (var dayList in groupedData.Values)
                {
                    foreach (var item in dayList) { item.LastPrice /= bolenDeger; }
                }
            }

            ViewBag.SelectedCurrency = currency;
            ViewBag.CurrencySymbol = sembol;

            return View(groupedData);
        }

        // Dinamik sayfa gösterimi için slug (url) ile sayfa bulma
        public IActionResult Page(string slug)
        {
            // Veritabanından gelen slug (url) ile eşleşen ve aktif olan menüyü/sayfayı buluyoruz
            var page = context.Menus.FirstOrDefault(m => m.Slug == slug && m.IsActive);

            if (page == null)
            {
                return RedirectToAction("Index"); // Sayfa yoksa anasayfaya at
            }

            return View(page); // Bulunan sayfayı View'a gönder
        }

        // --- GRAFİK İÇİN EKLENEN METOT ---
        [HttpGet]
        public IActionResult GetChartData(string name = "gram-altin")
        {
            // Son 24 saatlik fiyat geçmişini getirir
            var limit = DateTime.Now.AddHours(-24);

            var history = context.MarketDatas
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

        // --- İLETİŞİM SAYFASI (GET) ---
        [HttpGet]
        public IActionResult Contact()
        {
            return View();
        }

        // --- İLETİŞİM FORMU MAİL GÖNDERME (POST) ---
        [HttpPost]
        public IActionResult Contact(Models.ContactViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // 1. SMTP Ayarları (Kendi mail adresin ve Uygulama Şifren ile değiştir)
                string smtpServer = "smtp.gmail.com";
                int smtpPort = 587;
                string smtpUser = "bilgi@ebiscube.com";
                string smtpPass = "google_uygulama_sifren_buraya";

                // 2. Kime gidecek? (Admin panelinden ayarladığın ContactEmail adresini alıyoruz)
                var settings = context.SiteSettings.FirstOrDefault();
                string toEmail = settings?.ContactEmail ?? "admin@altinzamani.com";

                // 3. Maili Gönderme İşlemi
                using (var client = new System.Net.Mail.SmtpClient(smtpServer, smtpPort))
                {
                    System.Net.NetworkCredential networkCredential = new(smtpUser, smtpPass);
                    client.Credentials = networkCredential;
                    client.EnableSsl = true;

                    var mailMessage = new System.Net.Mail.MailMessage
                    {
                        From = new System.Net.Mail.MailAddress(smtpUser, "Altın Zamanı Web Formu"),
                        Subject = $"Yeni İletişim Mesajı: {model.Subject}",
                        Body = $"<strong>Gönderen Adı:</strong> {model.Name} <br/>" +
                               $"<strong>E-Posta:</strong> {model.Email} <br/><br/>" +
                               $"<strong>Mesaj:</strong> <br/> {model.Message}",
                        IsBodyHtml = true
                    };

                    mailMessage.To.Add(toEmail);
                    client.Send(mailMessage); // Maili fırlat!
                }

                TempData["SuccessMessage"] = "Mesajınız başarıyla gönderildi! En kısa sürede size dönüş yapacağız.";
                return RedirectToAction("Contact");
            }
            catch (Exception ex)
            {
                // Mail gönderiminde hata olursa sayfada göster
                ViewBag.ErrorMessage = $"Mesaj gönderilirken sunucu kaynaklı bir hata oluştu: {ex.Message}";
                return View(model);
            }
        }
    }
}