using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ShopeeVoucherWeb.Helpers;
using System.Net.Http.Headers;
using System.Text.Json;

namespace ShopeeVoucherWeb.Pages.Admin
{
    public class IndexModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public IndexModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public AdminStatsViewModel Stats { get; set; } = new();

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
                var response = await client.GetAsync("api/Admin/Stats");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    Stats = JsonSerializer.Deserialize<AdminStatsViewModel>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new AdminStatsViewModel();
                }
            }
            catch
            {
                TempData["ErrorMessage"] = "Failed to load admin dashboard.";
            }

            return Page();
        }

        private bool CheckAdminAccess()
        {
            var token = HttpContext.Session.GetString("JWTToken");
            if (string.IsNullOrEmpty(token)) return false;

            var roles = HttpContext.Session.GetObjectFromJson<List<string>>("UserRoles");
            return roles != null && roles.Contains("Admin");
        }
    }

    public class AdminStatsViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalVouchers { get; set; }
        public int ActiveVouchers { get; set; }
        public int ExpiredVouchers { get; set; }
        public int TotalSavedVouchers { get; set; }
    }
}