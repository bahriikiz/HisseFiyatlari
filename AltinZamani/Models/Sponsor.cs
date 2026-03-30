using System.ComponentModel.DataAnnotations;

namespace AltinZamani.Models
{
    public class Sponsor
    {
        [Key]
        public int Id { get; set; }
        public string? SiteType { get; set; }
        public string? Name { get; set; } // Sponsor adı [cite: 41]
        public string? LogoUrl { get; set; } // Sponsor logo [cite: 40]
        public string? TargetUrl { get; set; } // Web sitesi linki [cite: 42]
        public int Order { get; set; }
        public bool IsActive { get; set; }
    }
}