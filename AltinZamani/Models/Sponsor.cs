using System.ComponentModel.DataAnnotations;

namespace AltinZamani.Models
{
    public class Sponsor
    {
        [Key]
        public int? Id { get; set; }

        [Display(Name = "Sponsor Adı")]
        public string? Name { get; set; }

        [Display(Name = "Sponsor Logo URL")]
        public string? LogoUrl { get; set; }

        [Display(Name = "Web Sitesi Linki")]
        public string? WebsiteLink { get; set; }

        [Required]
        [Display(Name = "Sıra Numarası")]
        public int? Order { get; set; }

        [Required] 
        [Display(Name = "Aktif mi?")]
        public bool? IsActive { get; set; }
    }
}