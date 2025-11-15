namespace ShopeeVoucherAPI.DTOs
{
    public class SavedVoucherDto
    {
        public int SavedId { get; set; }
        public int UserId { get; set; }
        public int VoucherId { get; set; }
        public DateTime? SavedAt { get; set; }
        public VoucherDto? Voucher { get; set; }
    }
}
