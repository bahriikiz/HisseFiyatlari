using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;


namespace AltinZamani.Models
{
    public class Partner
    {
        [Key]
        public int? Id { get; set; }

        [Required(ErrorMessage = "Partner/Site adı zorunludur.")]
        [Display(Name = "Site Adı")]
        public string Name { get; set; }

        [Display(Name = "Hedef URL (Link)")]
        public string? Url { get; set; }

        [Display(Name = "Görsel Linki veya Bootstrap İkonu (Örn: bi-bank)")]
        public string? IconOrImageUrl { get; set; }
       
        [NotMapped]
        public IFormFile? ImageFile { get; set; }

        [Display(Name = "Rozet Yazısı (Örn: Kardeş Site, Yakında)")]
        public string? BadgeText { get; set; }

        [Display(Name = "Rozet Rengi (success, primary, warning, danger)")]
        public string? BadgeColor { get; set; }

        [Display(Name = "Sıralama")]
        public int? Order { get; set; }

        [Display(Name = "Sitede Görünsün mü?")]
        public bool IsActive { get; set; } = true;
    }
}