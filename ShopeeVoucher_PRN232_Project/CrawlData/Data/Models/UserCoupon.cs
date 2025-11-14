using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CrawlData.Data.Models
{
    [Table("user_coupon")]
    public class UserCoupon
    {
        [Key]
        [Column("userid", Order = 0)]
        public int UserId { get; set; }

        [Key]
        [Column("couponid", Order = 1)]
        public int CouponId { get; set; }

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [ForeignKey("CouponId")]
        public virtual Coupon? Coupon { get; set; }
    }
}

