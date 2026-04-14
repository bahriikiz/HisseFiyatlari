using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http; // DOSYA YÜKLEME İÇİN GEREKLİ

namespace AltinZamani.Models
{
    public class Sponsor
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Sponsor Adı zorunludur.")]
        [Display(Name = "Sponsor Adı")]
        public string Name { get; set; }

        [Display(Name = "Sponsor Site Linki (URL)")]
        public string? WebsiteLink { get; set; }

        [Display(Name = "Logo Linki")]
        public string? LogoUrl { get; set; }

        [NotMapped]
        [Display(Name = "Bilgisayardan Logo Seç")]
        public IFormFile? ImageFile { get; set; }

        [Display(Name = "Sıralama")]
        public int Order { get; set; }

        [Display(Name = "Aktif mi?")]
        public bool IsActive { get; set; } = true;
    }
}