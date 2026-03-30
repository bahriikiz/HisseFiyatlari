using System.ComponentModel.DataAnnotations;

namespace AltinZamani.Models
{
    public class SiteSetting
    {
        [Key]
        public int Id { get; set; }
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
        public int DataRetentionsDays { get; set; } = 5;
        public int DataRetentionDays { get; internal set; }
    }
}