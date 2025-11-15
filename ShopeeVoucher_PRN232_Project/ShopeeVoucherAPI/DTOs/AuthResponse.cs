namespace ShopeeVoucherAPI.DTOs
{
    public class AuthResponse
    {
        public string Token { get; set; } = string.Empty;
        public UserDto User { get; set; } = null!;
        public string Message { get; set; } = string.Empty;
    }
}