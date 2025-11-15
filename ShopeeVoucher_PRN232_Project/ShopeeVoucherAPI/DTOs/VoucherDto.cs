namespace ShopeeVoucherAPI.DTOs
{
    public class VoucherDto
    {
        public int VoucherId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string VoucherCode { get; set; } = string.Empty;
        public string? DiscountValue { get; set; }
        public decimal? MinOrder { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string? Link { get; set; }
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public string Color { get; set; } = "#667eea";
    }
}
