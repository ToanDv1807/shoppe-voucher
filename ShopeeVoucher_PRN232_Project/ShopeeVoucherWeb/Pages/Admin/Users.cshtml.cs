using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ShopeeVoucherWeb.Helpers;
using System.Net.Http.Headers;
using System.Text.Json;

namespace ShopeeVoucherWeb.Pages.Admin
{
    [IgnoreAntiforgeryToken]
    public class UsersModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<UsersModel> _logger;

        public UsersModel(IHttpClientFactory httpClientFactory, ILogger<UsersModel> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public List<UserViewModel> Users { get; set; } = new();
        public int CurrentUserId { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!CheckAdminAccess())
            {
                TempData["ErrorMessage"] = "Access denied. Admin privileges required.";
                return RedirectToPage("/Vouchers/Index");
            }

            // Get current user ID from session
            var userIdString = HttpContext.Session.GetString("UserId");
            if (!string.IsNullOrEmpty(userIdString) && int.TryParse(userIdString, out int userId))
            {
                CurrentUserId = userId;
            }

            var token = HttpContext.Session.GetString("JWTToken");
            var client = _httpClientFactory.CreateClient("api");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!);

            try
            {
                var response = await client.GetAsync("api/Admin/Users");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    Users = JsonSerializer.Deserialize<List<UserViewModel>>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new List<UserViewModel>();
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to load users.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading users");
                TempData["ErrorMessage"] = "Failed to load users.";
            }

            return Page();
        }

        public async Task<IActionResult> OnPutToggleRoleAsync()
        {
            _logger.LogInformation("=== OnPutToggleRoleAsync START ===");

            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();

            _logger.LogInformation("Request body: {Body}", body);

            if (!int.TryParse(body, out int userId))
            {
                _logger.LogWarning("Invalid user ID: {Body}", body);
                return new JsonResult(new { success = false, message = "Invalid user ID" });
            }

            var token = HttpContext.Session.GetString("JWTToken");
            var client = _httpClientFactory.CreateClient("api");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!);

            try
            {
                _logger.LogInformation("Calling API: PUT /api/Admin/Users/{UserId}/ToggleRole", userId);

                var response = await client.PutAsync($"api/Admin/Users/{userId}/ToggleRole", null);
                var responseBody = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("API Response Status: {StatusCode}", response.StatusCode);
                _logger.LogInformation("API Response Body: {Body}", responseBody);

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ToggleRoleResponse>(responseBody, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    return new JsonResult(new { success = true, message = result?.Message ?? "Role updated", roles = result?.Roles });
                }
                else
                {
                    // Try to parse error response
                    try
                    {
                        var error = JsonSerializer.Deserialize<ErrorResponse>(responseBody, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                        return new JsonResult(new { success = false, message = error?.Message ?? "Failed to toggle role" });
                    }
                    catch
                    {
                        // If JSON parsing fails, return raw response
                        return new JsonResult(new { success = false, message = responseBody });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling user role");
                return new JsonResult(new { success = false, message = "An error occurred" });
            }
        }

        public async Task<IActionResult> OnDeleteUserAsync()
        {
            _logger.LogInformation("=== OnDeleteUserAsync START ===");

            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();

            _logger.LogInformation("Request body: {Body}", body);

            if (!int.TryParse(body, out int userId))
            {
                _logger.LogWarning("Invalid user ID: {Body}", body);
                return new JsonResult(new { success = false, message = "Invalid user ID" });
            }

            var token = HttpContext.Session.GetString("JWTToken");
            var client = _httpClientFactory.CreateClient("api");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!);

            try
            {
                _logger.LogInformation("Calling API: DELETE /api/Admin/Users/{UserId}", userId);

                var response = await client.DeleteAsync($"api/Admin/Users/{userId}");
                
                _logger.LogInformation("API Response Status: {StatusCode}", response.StatusCode);

                // ✅ FIX: Read response body and check if it's JSON
                var responseBody = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("API Response Body: {Body}", responseBody);

                if (response.IsSuccessStatusCode)
                {
                    // ✅ Try to parse JSON response, fallback to default message
                    try
                    {
                        var result = JsonSerializer.Deserialize<JsonElement>(responseBody, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        string message = "User deleted successfully";
                        if (result.TryGetProperty("message", out JsonElement messageElement))
                        {
                            message = messageElement.GetString() ?? message;
                        }

                        return new JsonResult(new { success = true, message });
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse JSON response, using default message. Response: {Response}", responseBody);
                        return new JsonResult(new { success = true, message = "User deleted successfully" });
                    }
                }
                else
                {
                    // ✅ Try to parse error response
                    try
                    {
                        var error = JsonSerializer.Deserialize<ErrorResponse>(responseBody, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                        return new JsonResult(new { success = false, message = error?.Message ?? "Failed to delete user" });
                    }
                    catch
                    {
                        // If JSON parsing fails, return raw response
                        return new JsonResult(new { success = false, message = responseBody });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user");
                return new JsonResult(new { success = false, message = "An error occurred while deleting user" });
            }
        }

        private bool CheckAdminAccess()
        {
            var token = HttpContext.Session.GetString("JWTToken");
            if (string.IsNullOrEmpty(token)) return false;

            var roles = HttpContext.Session.GetObjectFromJson<List<string>>("UserRoles");
            return roles != null && roles.Contains("Admin");
        }
    }

    public class UserViewModel
    {
        public int UserId { get; set; }
        public string? FullName { get; set; }
        public string Email { get; set; } = string.Empty;
        public DateTime? CreatedAt { get; set; }
        public List<string> Roles { get; set; } = new();
    }

    public class ToggleRoleResponse
    {
        public string Message { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new();
    }

    public class ErrorResponse
    {
        public string Message { get; set; } = string.Empty;
    }
}