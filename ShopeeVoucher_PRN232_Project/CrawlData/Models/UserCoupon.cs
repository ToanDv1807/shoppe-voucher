using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CrawlData.Models
{
    [Table("user_coupon")]
    public class UserCoupon
    {
        [Column("userid")]
        public int UserId { get; set; }

        [Column("couponid")]
        public int CouponId { get; set; }

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        [ForeignKey("CouponId")]
        public virtual Coupon Coupon { get; set; } = null!;
    }
}

