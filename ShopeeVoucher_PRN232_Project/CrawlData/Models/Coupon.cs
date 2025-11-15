using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CrawlData.Models
{
    [Table("coupon")]
    public class Coupon
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("type")]
        public bool Type { get; set; }

        [Column("supplier")]
        [MaxLength(50)]
        public string? Supplier { get; set; }

        [Required]
        [Column("discount")]
        public double Discount { get; set; }

        [Column("min_value_apply")]
        public double? MinValueApply { get; set; }

        [Column("description")]
        [MaxLength(10000)]
        public string? Description { get; set; }

        [Required]
        [Column("start_date")]
        public DateTime StartDate { get; set; }

        [Column("available")]
        public double? Available { get; set; }

        [Required]
        [Column("expired_date")]
        public DateTime ExpiredDate { get; set; }

        [Column("url_apply_list")]
        [MaxLength(255)]
        public string? UrlApplyList { get; set; }

        [Column("code")]
        [MaxLength(255)]
        public string? Code { get; set; }

        [Required]
        [Column("platform")]
        [MaxLength(50)]
        public string Platform { get; set; } = string.Empty;

        // Navigation property
        public virtual ICollection<UserCoupon> UserCoupons { get; set; } = new List<UserCoupon>();
    }
}

