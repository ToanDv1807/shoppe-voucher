using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ShopeeVoucherWeb.Helpers;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;

namespace ShopeeVoucherWeb.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public RegisterModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required(ErrorMessage = "Full name is required")]
            [StringLength(100, ErrorMessage = "Full name cannot exceed 100 characters")]
            [Display(Name = "Full Name")]
            public string FullName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Email is required")]
            [EmailAddress(ErrorMessage = "Invalid email address")]
            [Display(Name = "Email")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Password is required")]
            [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long")]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; } = string.Empty;

            [Required(ErrorMessage = "Confirm password is required")]
            [DataType(DataType.Password)]
            [Compare("Password", ErrorMessage = "Passwords do not match")]
            [Display(Name = "Confirm Password")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        public IActionResult OnGet()
        {
            // Check if already logged in
            var token = HttpContext.Session.GetString("JWTToken");
            if (!string.IsNullOrEmpty(token))
            {
                return RedirectToPage("/Vouchers/Index");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                var client = _httpClientFactory.CreateClient("api");
                var registerRequest = new
                {
                    fullName = Input.FullName,
                    email = Input.Email,
                    password = Input.Password
                };

                var json = JsonSerializer.Serialize(registerRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync("api/Account/Register", content);

                if (response.IsSuccessStatusCode)
                {
                    var resultJson = await response.Content.ReadAsStringAsync();
                    var authResponse = JsonSerializer.Deserialize<AuthResponse>(resultJson, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (authResponse != null)
                    {
                        // Auto login after registration
                        HttpContext.Session.SetString("JWTToken", authResponse.Token);
                        HttpContext.Session.SetString("UserEmail", authResponse.User.Email);
                        HttpContext.Session.SetString("UserName", authResponse.User.FullName ?? "");
                        HttpContext.Session.SetInt32("UserId", authResponse.User.UserId);
                        HttpContext.Session.SetObjectAsJson("UserRoles", authResponse.User.Roles);

                        TempData["SuccessMessage"] = "Registration successful! Welcome to Coupon Hub.";
                        return RedirectToPage("/Vouchers/Index");
                    }
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    ModelState.AddModelError(string.Empty, "Registration failed. Email may already be in use.");
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "An error occurred during registration. Please try again.");
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