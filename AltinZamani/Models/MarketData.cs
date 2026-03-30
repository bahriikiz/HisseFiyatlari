using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AltinZamani.Models
{
    public class MarketData
    {
        [Key]
        public int Id { get; set; }

        public string? SiteType { get; set; }
        public string? Name { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal LastPrice { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal ChangePercentage { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal ChangeAmount { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal? VolumeAmount { get; set; }

        public int? VolumeCount { get; set; }
        public DateTime RecordDate { get; set; }
    }
}