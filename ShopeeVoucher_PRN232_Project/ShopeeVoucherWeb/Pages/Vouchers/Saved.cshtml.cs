using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ShopeeVoucherWeb.Models;
using System.Net.Http.Headers;
using System.Text.Json;

namespace ShopeeVoucherWeb.Pages.Vouchers
{
    [IgnoreAntiforgeryToken] // ✅ Add class-level attribute
    public class SavedModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<SavedModel> _logger; // ✅ Add logger

        public SavedModel(IHttpClientFactory httpClientFactory, ILogger<SavedModel> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public List<CouponViewModel> SavedCoupons { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            _logger.LogInformation("=== Saved.OnGetAsync START ===");

            var token = HttpContext.Session.GetString("JWTToken");
            _logger.LogInformation("Token exists: {TokenExists}", !string.IsNullOrEmpty(token));

            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("No token, redirecting to login");
                return RedirectToPage("/Account/Login");
            }

            var client = _httpClientFactory.CreateClient("api");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                _logger.LogInformation("Calling API: api/SavedVoucher");
                var response = await client.GetAsync("api/SavedVoucher");

                _logger.LogInformation("API Response Status: {StatusCode}", response.StatusCode);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("Response JSON length: {Length}", json.Length);
                    _logger.LogInformation("Response JSON: {Json}", json);

                    var savedVouchers = JsonSerializer.Deserialize<List<SavedVoucherDto>>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    _logger.LogInformation("Deserialized {Count} saved vouchers", savedVouchers?.Count ?? 0);

                    SavedCoupons = savedVouchers?
                        .Where(sv => sv.Voucher != null)
                        .Select(sv => new CouponViewModel
                        {
                            VoucherId = sv.Voucher.VoucherId,
                            Title = sv.Voucher.Title,
                            Description = sv.Voucher.Description,
                            Code = sv.Voucher.VoucherCode,
                            ExpiredDate = sv.Voucher.ExpiryDate ?? DateTime.Now.AddDays(30),
                            Link = sv.Voucher.Link ?? "#",
                            Color = sv.Voucher.Color,
                            CategoryName = sv.Voucher.CategoryName,
                            LogoUrl = string.Empty, // ✅ ADD THIS!
                            IsSaved = true
                        }).ToList() ?? new List<CouponViewModel>();

                    _logger.LogInformation("Mapped {Count} CouponViewModels", SavedCoupons.Count);
                }
                else
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("API returned non-success: {StatusCode}, Body: {Body}", 
                        response.StatusCode, errorBody);
                    TempData["ErrorMessage"] = $"Failed to load saved vouchers: {response.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception loading saved vouchers");
                TempData["ErrorMessage"] = "Failed to load saved vouchers.";
            }

            _logger.LogInformation("Returning page with {Count} saved coupons", SavedCoupons.Count);
            return Page();
        }

        public async Task<IActionResult> OnDeleteRemoveVoucherAsync()
        {
            _logger.LogInformation("=== OnDeleteRemoveVoucherAsync START ===");

            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();
            
            _logger.LogInformation("Raw request body: {Body}", body);

            int voucherId;
            try
            {
                voucherId = JsonSerializer.Deserialize<int>(body);
                _logger.LogInformation("Deserialized VoucherId: {VoucherId}", voucherId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deserialize voucherId");
                return new JsonResult(new { success = false, message = "Invalid voucher ID format" });
            }

            var token = HttpContext.Session.GetString("JWTToken");
            if (string.IsNullOrEmpty(token))
            {
                return new JsonResult(new { success = false, message = "Please login" });
            }

            var client = _httpClientFactory.CreateClient("api");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await client.DeleteAsync($"api/SavedVoucher/{voucherId}");
                var responseBody = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("Delete response: {StatusCode}, Body: {Body}", 
                    response.StatusCode, responseBody);

                if (response.IsSuccessStatusCode)
                {
                    return new JsonResult(new { success = true, message = "Voucher removed successfully" });
                }
                else
                {
                    return new JsonResult(new { success = false, message = responseBody });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception removing voucher");
                return new JsonResult(new { success = false, message = "An error occurred" });
            }
        }
    }

    public class SavedVoucherDto
    {
        public int SavedId { get; set; }
        public int UserId { get; set; }
        public int VoucherId { get; set; }
        public DateTime? SavedAt { get; set; }
        public VoucherDto? Voucher { get; set; }
    }
}