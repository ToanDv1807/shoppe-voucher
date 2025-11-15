using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ShopeeVoucherWeb.Models;
using System.Net.Http.Headers;
using System.Text.Json;

namespace ShopeeVoucherWeb.Pages.Vouchers
{
    [IgnoreAntiforgeryToken] // ✅ Di chuyển lên đây - class level
    public class IndexModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(IHttpClientFactory httpClientFactory, ILogger<IndexModel> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public List<CouponViewModel> Coupons { get; set; } = new();
        public bool IsLoggedIn { get; set; }
        public string? UserName { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            IsLoggedIn = !string.IsNullOrEmpty(HttpContext.Session.GetString("JWTToken"));
            UserName = HttpContext.Session.GetString("UserName");

            _logger.LogInformation("=== OnGetAsync - IsLoggedIn: {IsLoggedIn}, UserName: {UserName} ===", IsLoggedIn, UserName);

            var client = _httpClientFactory.CreateClient("api");

            try
            {
                var response = await client.GetAsync("api/Voucher");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var vouchers = JsonSerializer.Deserialize<List<VoucherDto>>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    Coupons = vouchers?.Select(v => new CouponViewModel
                    {
                        VoucherId = v.VoucherId,
                        Title = v.Title,
                        Description = v.Description,
                        Code = v.VoucherCode,
                        ExpiredDate = v.ExpiryDate ?? DateTime.Now.AddDays(30),
                        Link = v.Link ?? "#",
                        Color = v.Color,
                        CategoryName = v.CategoryName,
                        LogoUrl = string.Empty,
                        IsSaved = false
                    }).ToList() ?? new List<CouponViewModel>();

                    _logger.LogInformation("Loaded {Count} vouchers", Coupons.Count);

                    var token = HttpContext.Session.GetString("JWTToken");
                    if (!string.IsNullOrEmpty(token))
                    {
                        await LoadSavedVouchersStatus(Coupons, token);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading vouchers");
                TempData["ErrorMessage"] = "Failed to load vouchers. Please try again.";
            }

            return Page();
        }

        // ❌ Removed [IgnoreAntiforgeryToken] from here
        public async Task<IActionResult> OnPostSaveVoucherAsync()
        {
            _logger.LogInformation("=== OnPostSaveVoucherAsync START ===");

            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();
            
            _logger.LogInformation("Raw request body: {Body}", body);

            if (string.IsNullOrWhiteSpace(body))
            {
                _logger.LogWarning("Request body is empty");
                return new JsonResult(new { success = false, message = "Invalid request body" });
            }

            int voucherId;
            try
            {
                voucherId = JsonSerializer.Deserialize<int>(body);
                _logger.LogInformation("Deserialized VoucherId: {VoucherId}", voucherId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deserialize voucherId from body: {Body}", body);
                return new JsonResult(new { success = false, message = "Invalid voucher ID format" });
            }

            _logger.LogInformation("VoucherId received: {VoucherId}", voucherId);
            _logger.LogInformation("VoucherId type: {Type}", voucherId.GetType().Name);

            var token = HttpContext.Session.GetString("JWTToken");
            _logger.LogInformation("Token exists: {TokenExists}", !string.IsNullOrEmpty(token));

            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("No JWT token found in session");
                return new JsonResult(new { success = false, message = "Please login to save vouchers" });
            }

            var client = _httpClientFactory.CreateClient("api");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var requestBody = JsonSerializer.Serialize(voucherId);
                _logger.LogInformation("Request body to API: {RequestBody}", requestBody);

                var content = new StringContent(requestBody, System.Text.Encoding.UTF8, "application/json");

                _logger.LogInformation("Sending POST to: {Url}", $"{client.BaseAddress}api/SavedVoucher");

                var response = await client.PostAsync("api/SavedVoucher", content);

                _logger.LogInformation("Response StatusCode: {StatusCode}", response.StatusCode);

                var responseBody = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Response Body: {ResponseBody}", responseBody);

                if (response.IsSuccessStatusCode)
                {
                    return new JsonResult(new { success = true, message = "Voucher saved successfully" });
                }
                else
                {
                    _logger.LogWarning("API returned non-success status: {StatusCode}, Body: {Body}", 
                        response.StatusCode, responseBody);
                    return new JsonResult(new { success = false, message = responseBody });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in OnPostSaveVoucherAsync");
                return new JsonResult(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }

        // ❌ Removed [IgnoreAntiforgeryToken] from here
        public async Task<IActionResult> OnDeleteUnsaveVoucherAsync()
        {
            _logger.LogInformation("=== OnDeleteUnsaveVoucherAsync START ===");

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

            _logger.LogInformation("=== OnDeleteUnsaveVoucherAsync - VoucherId: {VoucherId} ===", voucherId);

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

                _logger.LogInformation("Delete response: {StatusCode}, Body: {Body}", response.StatusCode, responseBody);

                if (response.IsSuccessStatusCode)
                {
                    return new JsonResult(new { success = true, message = "Voucher removed from saved" });
                }
                else
                {
                    return new JsonResult(new { success = false, message = responseBody });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnDeleteUnsaveVoucherAsync");
                return new JsonResult(new { success = false, message = "An error occurred" });
            }
        }

        private async Task LoadSavedVouchersStatus(List<CouponViewModel> coupons, string token)
        {
            var client = _httpClientFactory.CreateClient("api");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            foreach (var coupon in coupons)
            {
                try
                {
                    var response = await client.GetAsync($"api/SavedVoucher/Check/{coupon.VoucherId}");
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        var result = JsonSerializer.Deserialize<CheckSavedResponse>(json, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                        coupon.IsSaved = result?.IsSaved ?? false;
                    }
                }
                catch { }
            }
        }
    }

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

    public class CheckSavedResponse
    {
        public bool IsSaved { get; set; }
    }
}