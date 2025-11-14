using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CrawlData.Data.Models
{
    [Table("coupon")]
    public class Coupon
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("platform")]
        [Required]
        public int Platform { get; set; }

        [Column("code")]
        [MaxLength(50)]
        public string? Code { get; set; }

        [Column("type")]
        public bool? Type { get; set; }

        [Column("discount")]
        public double? Discount { get; set; }

        [Column("min_cart_value")]
        public double? MinCartValue { get; set; }

        [Column("start_date")]
        public DateTime? StartDate { get; set; }

        [Column("expired_date")]
        public DateTime? ExpiredDate { get; set; }

        [Column("description")]
        [MaxLength(255)]
        public string? Description { get; set; }

        // Additional fields from crawled data (matching CouponData class)
        [Column("supplier")]
        [MaxLength(200)]
        public string? Supplier { get; set; }

        [Column("supplier_logo")]
        [MaxLength(500)]
        public string? SupplierLogo { get; set; }

        [Column("discount_percent")]
        [MaxLength(50)]
        public string? DiscountPercent { get; set; }

        [Column("minimum_order")]
        [MaxLength(100)]
        public string? MinimumOrder { get; set; }

        [Column("note")]
        [MaxLength(1000)]
        public string? Note { get; set; }

        [Column("apply_link")]
        [MaxLength(1000)]
        public string? ApplyLink { get; set; }

        [Column("banner_link")]
        [MaxLength(1000)]
        public string? BannerLink { get; set; }

        [Column("category")]
        [MaxLength(200)]
        public string? Category { get; set; }

        // New fields for discount type and code
        [Column("is_percent_discount")]
        public bool IsPercentDiscount { get; set; }

        [Column("discount_value")]
        public double? DiscountValue { get; set; }

        [Column("coupon_code")]
        [MaxLength(50)]
        public string? CouponCode { get; set; }

        // Foreign key navigation
        [ForeignKey("Platform")]
        public virtual Ecommerce? Ecommerce { get; set; }

        // Navigation property
        public virtual ICollection<UserCoupon> UserCoupons { get; set; } = new List<UserCoupon>();
    }
}

