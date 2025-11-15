namespace ShopeeVoucherWeb.Models
{
    public class CouponViewModel
    {
        public int VoucherId { get; set; }
        public string LogoUrl { get; set; } = string.Empty; // ✅ Default value
        public string Color { get; set; } = "#667eea"; // ✅ Default value
        public string Title { get; set; } = string.Empty; // ✅ Default value
        public string Description { get; set; } = string.Empty; // ✅ Default value
        public DateTime ExpiredDate { get; set; }
        public string Link { get; set; } = string.Empty; // ✅ Default value
        public string Code { get; set; } = string.Empty; // ✅ Default value
        public string? CategoryName { get; set; }
        public bool IsSaved { get; set; }
    }
}
