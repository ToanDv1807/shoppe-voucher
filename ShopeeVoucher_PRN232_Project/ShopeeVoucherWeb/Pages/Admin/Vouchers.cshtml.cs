using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ShopeeVoucherWeb.Helpers;
using System.Net.Http.Headers;
using System.Text.Json;

namespace ShopeeVoucherWeb.Pages.Admin
{
    [IgnoreAntiforgeryToken]
    public class VouchersModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<VouchersModel> _logger;

        public VouchersModel(IHttpClientFactory httpClientFactory, ILogger<VouchersModel> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public List<AdminVoucherViewModel> Vouchers { get; set; } = new();
        public List<CategoryViewModel> Categories { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            if (!CheckAdminAccess())
            {
                TempData["ErrorMessage"] = "Access denied. Admin privileges required.";
                return RedirectToPage("/Vouchers/Index");
            }

            var token = HttpContext.Session.GetString("JWTToken");
            var client = _httpClientFactory.CreateClient("api");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!);

            try
            {
                // Load vouchers
                var vouchersResponse = await client.GetAsync("api/Admin/Vouchers");
                if (vouchersResponse.IsSuccessStatusCode)
                {
                    var json = await vouchersResponse.Content.ReadAsStringAsync();
                    Vouchers = JsonSerializer.Deserialize<List<AdminVoucherViewModel>>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new List<AdminVoucherViewModel>();
                }

                // Load categories
                var categoriesResponse = await client.GetAsync("api/Admin/Categories");
                if (categoriesResponse.IsSuccessStatusCode)
                {
                    var json = await categoriesResponse.Content.ReadAsStringAsync();
                    Categories = JsonSerializer.Deserialize<List<CategoryViewModel>>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new List<CategoryViewModel>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading vouchers");
                TempData["ErrorMessage"] = "Failed to load vouchers.";
            }

            return Page();
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            _logger.LogInformation("=== OnPostCreateAsync START ===");

            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();
            _logger.LogInformation("Request body: {Body}", body);

            var token = HttpContext.Session.GetString("JWTToken");
            var client = _httpClientFactory.CreateClient("api");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!);

            try
            {
                var content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
                var response = await client.PostAsync("api/Admin/Vouchers", content);
                var responseBody = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("API Response: {StatusCode}, {Body}", response.StatusCode, responseBody);

                if (response.IsSuccessStatusCode)
                {
                    return new JsonResult(new { success = true, message = "Voucher created successfully" });
                }
                else
                {
                    var error = TryParseError(responseBody);
                    return new JsonResult(new { success = false, message = error });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating voucher");
                return new JsonResult(new { success = false, message = "An error occurred" });
            }
        }

        public async Task<IActionResult> OnPutUpdateAsync()
        {
            _logger.LogInformation("=== OnPutUpdateAsync START ===");

            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();
            _logger.LogInformation("Request body: {Body}", body);

            var updateDto = JsonSerializer.Deserialize<UpdateVoucherRequest>(body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (updateDto == null || updateDto.VoucherId <= 0)
            {
                return new JsonResult(new { success = false, message = "Invalid voucher data" });
            }

            var token = HttpContext.Session.GetString("JWTToken");
            var client = _httpClientFactory.CreateClient("api");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!);

            try
            {
                var content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
                var response = await client.PutAsync($"api/Admin/Vouchers/{updateDto.VoucherId}", content);
                var responseBody = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("API Response: {StatusCode}, {Body}", response.StatusCode, responseBody);

                if (response.IsSuccessStatusCode)
                {
                    return new JsonResult(new { success = true, message = "Voucher updated successfully" });
                }
                else
                {
                    var error = TryParseError(responseBody);
                    return new JsonResult(new { success = false, message = error });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating voucher");
                return new JsonResult(new { success = false, message = "An error occurred" });
            }
        }

        public async Task<IActionResult> OnDeleteVoucherAsync()
        {
            _logger.LogInformation("=== OnDeleteVoucherAsync START ===");

            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();

            if (!int.TryParse(body, out int voucherId))
            {
                return new JsonResult(new { success = false, message = "Invalid voucher ID" });
            }

            var token = HttpContext.Session.GetString("JWTToken");
            var client = _httpClientFactory.CreateClient("api");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!);

            try
            {
                var response = await client.DeleteAsync($"api/Admin/Vouchers/{voucherId}");
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return new JsonResult(new { success = true, message = "Voucher deleted successfully" });
                }
                else
                {
                    var error = TryParseError(responseBody);
                    return new JsonResult(new { success = false, message = error });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting voucher");
                return new JsonResult(new { success = false, message = "An error occurred" });
            }
        }

        public async Task<IActionResult> OnPutToggleActiveAsync()
        {
            _logger.LogInformation("=== OnPutToggleActiveAsync START ===");

            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();

            if (!int.TryParse(body, out int voucherId))
            {
                return new JsonResult(new { success = false, message = "Invalid voucher ID" });
            }

            var token = HttpContext.Session.GetString("JWTToken");
            var client = _httpClientFactory.CreateClient("api");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!);

            try
            {
                var response = await client.PutAsync($"api/Admin/Vouchers/{voucherId}/ToggleActive", null);
                var responseBody = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("API Response: {Body}", responseBody);

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ToggleActiveResponse>(responseBody, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    return new JsonResult(new
                    {
                        success = true,
                        message = result?.Message ?? "Status updated",
                        isActive = result?.IsActive ?? false
                    });
                }
                else
                {
                    var error = TryParseError(responseBody);
                    return new JsonResult(new { success = false, message = error });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling voucher status");
                return new JsonResult(new { success = false, message = "An error occurred" });
            }
        }

        private bool CheckAdminAccess()
        {
            var token = HttpContext.Session.GetString("JWTToken");
            if (string.IsNullOrEmpty(token)) return false;

            var roles = HttpContext.Session.GetObjectFromJson<List<string>>("UserRoles");
            return roles != null && roles.Contains("Admin");
        }

        private string TryParseError(string responseBody)
        {
            try
            {
                var error = JsonSerializer.Deserialize<ErrorResponse>(responseBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return error?.Message ?? responseBody;
            }
            catch
            {
                return responseBody;
            }
        }
    }

    public class AdminVoucherViewModel
    {
        public int VoucherId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string VoucherCode { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? DiscountValue { get; set; }
        public decimal? MinOrder { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string? Link { get; set; }
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public bool IsActive { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class CategoryViewModel
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
    }

    public class UpdateVoucherRequest
    {
        public int VoucherId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string VoucherCode { get; set; } = string.Empty;
        public string? DiscountValue { get; set; }
        public decimal? MinOrder { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string? Link { get; set; }
        public int CategoryId { get; set; }
    }

    public class ToggleActiveResponse
    {
        public string Message { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}