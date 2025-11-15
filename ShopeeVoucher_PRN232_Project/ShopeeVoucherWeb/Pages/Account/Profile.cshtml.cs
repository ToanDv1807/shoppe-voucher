using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ShopeeVoucherWeb.Helpers;
using ShopeeVoucherWeb.Models;

namespace ShopeeVoucherWeb.Pages.Account
{
    public class ProfileModel : PageModel
    {
        public UserProfileViewModel UserProfile { get; set; } = new();

        public IActionResult OnGet()
        {
            var token = HttpContext.Session.GetString("JWTToken");
            if (string.IsNullOrEmpty(token))
            {
                return RedirectToPage("/Account/Login");
            }

            var userName = HttpContext.Session.GetString("UserName");
            var userEmail = HttpContext.Session.GetString("UserEmail");
            var roles = HttpContext.Session.GetObjectFromJson<List<string>>("UserRoles");
            var userId = HttpContext.Session.GetInt32("UserId");

            UserProfile = new UserProfileViewModel
            {
                FullName = userName ?? "",
                Email = userEmail ?? "",
                Roles = roles ?? new List<string>()
            };

            return Page();
        }

        public IActionResult OnPostLogout()
        {
            HttpContext.Session.Clear();
            TempData["SuccessMessage"] = "You have been logged out successfully.";
            return RedirectToPage("/Vouchers/Index");
        }
    }
}