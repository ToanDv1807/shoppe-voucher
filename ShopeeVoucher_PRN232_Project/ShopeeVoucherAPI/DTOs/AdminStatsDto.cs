namespace ShopeeVoucherAPI.DTOs
{
    public class AdminStatsDto
    {
        public int TotalUsers { get; set; }
        public int TotalVouchers { get; set; }
        public int ActiveVouchers { get; set; }
        public int ExpiredVouchers { get; set; }
        public int TotalSavedVouchers { get; set; }
    }
}
