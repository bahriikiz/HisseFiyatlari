using System.ComponentModel.DataAnnotations;

namespace AltinZamani.Models
{
    public class Menu
    {
        [Key]
        public int Id { get; set; }
        public string? SiteType { get; set; }
        public string? Title { get; set; } // Ana Sayfa, Hakkımızda vb. [cite: 44]
        public string? Slug { get; set; } // URL yapısı (orn: hakkimizda)
        public string? Content { get; set; } // Sayfa içeriği [cite: 45]
        public int Order { get; set; } // Menü sırası
        public bool IsActive { get; set; }
    }
}