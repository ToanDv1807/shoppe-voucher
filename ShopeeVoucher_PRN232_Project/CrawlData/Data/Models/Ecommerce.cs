using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CrawlData.Data.Models
{
    [Table("ecommerce")]
    public class Ecommerce
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("Name")]
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        // Navigation property
        public virtual ICollection<Coupon> Coupons { get; set; } = new List<Coupon>();
    }
}

