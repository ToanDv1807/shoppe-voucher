using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ShopeeVoucherWeb.Helpers;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;

namespace ShopeeVoucherWeb.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public LoginModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? ReturnUrl { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Email is required")]
            [EmailAddress(ErrorMessage = "Invalid email address")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Password is required")]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            public bool RememberMe { get; set; }
        }

        public IActionResult OnGet(string? returnUrl = null)
        {
            // Check if already logged in
            var token = HttpContext.Session.GetString("JWTToken");
            if (!string.IsNullOrEmpty(token))
            {
                return RedirectToPage("/Vouchers/Index");
            }

            ReturnUrl = returnUrl;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            ReturnUrl = returnUrl;

            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                var client = _httpClientFactory.CreateClient("api");
                var loginRequest = new
                {
                    email = Input.Email,
                    password = Input.Password
                };

                var json = JsonSerializer.Serialize(loginRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync("api/Account/Login", content);

                if (response.IsSuccessStatusCode)
                {
                    var resultJson = await response.Content.ReadAsStringAsync();
                    var authResponse = JsonSerializer.Deserialize<AuthResponse>(resultJson, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (authResponse != null)
                    {
                        // Store JWT token and user info in session
                        HttpContext.Session.SetString("JWTToken", authResponse.Token);
                        HttpContext.Session.SetString("UserEmail", authResponse.User.Email);
                        HttpContext.Session.SetString("UserName", authResponse.User.FullName ?? "");
                        HttpContext.Session.SetInt32("UserId", authResponse.User.UserId);
                        HttpContext.Session.SetObjectAsJson("UserRoles", authResponse.User.Roles);

                        TempData["SuccessMessage"] = "Login successful! Welcome back.";

                        // Redirect to return URL or home
                        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                        {
                            return LocalRedirect(returnUrl);
                        }
                        return RedirectToPage("/Vouchers/Index");
                    }
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid email or password");
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "An error occurred during login. Please try again.");
            }

            return Page();
        }

        public class AuthResponse
        {
            public string Token { get; set; } = string.Empty;
            public UserDto User { get; set; } = null!;
            public string Message { get; set; } = string.Empty;
        }

        public class UserDto
        {
            public int UserId { get; set; }
            public string? FullName { get; set; }
            public string Email { get; set; } = string.Empty;
            public DateTime? CreatedAt { get; set; }
            public List<string> Roles { get; set; } = new();
        }
    }
}