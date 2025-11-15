using System.ComponentModel.DataAnnotations;

namespace ShopeeVoucherAPI.DTOs
{
    public class CreateVoucherDto
    {
        [Required(ErrorMessage = "Title is required")]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Voucher code is required")]
        [MaxLength(100)]
        public string VoucherCode { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? DiscountValue { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Minimum order must be positive")]
        public decimal? MinOrder { get; set; }

        public DateTime? ExpiryDate { get; set; }

        [MaxLength(500)]
        [Url(ErrorMessage = "Invalid URL format")]
        public string? Link { get; set; }

        [Required(ErrorMessage = "Category is required")]
        public int CategoryId { get; set; }
    }
}