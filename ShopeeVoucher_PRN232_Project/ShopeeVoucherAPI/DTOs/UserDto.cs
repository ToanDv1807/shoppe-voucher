namespace ShopeeVoucherAPI.DTOs
{
    public class UserDto
    {
        public int UserId { get; set; }
        public string? FullName { get; set; }
        public string Email { get; set; } = string.Empty;
        public DateTime? CreatedAt { get; set; }
        public List<string> Roles { get; set; } = new();
    }
}
