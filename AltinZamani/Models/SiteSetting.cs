using System.ComponentModel.DataAnnotations;

namespace AltinZamani.Models
{
    public class SiteSetting
    {
        [Key]
        public int? Id { get; set; }
        public string? SiteType { get; set; }
        public string? AdsenseCode { get; set; }
        public string? AnalyticsCode { get; set; }
        public string? ContactEmail { get; set; }
        public string? FooterText { get; set; }
        public string? FacebookUrl { get; set; }
        public bool IsFacebookActive { get; set; }
        public string? TwitterUrl { get; set; }
        public bool IsTwitterActive { get; set; }
        public string? InstagramUrl { get; set; }
        public bool IsInstagramActive { get; set; }
        public int ApiFetchIntervalInHours { get; set; } = 2;
        public int DataRetentionDays { get; set; } = 5;
        public int CleanupIntervalInHours { get; set; } = 24;
        public string? BannerAdCode { get; set; }
        public string? TopAdCode { get; set; }       // En üst (Navbar altı)
        public string? LeftAdCode { get; set; }      // Sol dikey reklam
        public string? RightAdCode { get; set; }     // Sağ dikey reklam
        // --- SEO Ayarları ---
        public string? MetaTitle { get; set; } = "Altın Zamanı - Canlı Altın ve Döviz Fiyatları";
        public string? MetaDescription { get; set; } = "En güncel altın fiyatları, çeyrek altın, gram altın ve canlı döviz kurlarını anlık takip edin.";
        public string? MetaKeywords { get; set; } = "altın, çeyrek altın, gram altın, döviz, canlı borsa, dolar, euro";

        // --- GÜVENLİK AYARLARI ---
        public string AdminUsername { get; set; } = "admin";
        public string AdminPassword { get; set; } = "altin2026";
    }
}