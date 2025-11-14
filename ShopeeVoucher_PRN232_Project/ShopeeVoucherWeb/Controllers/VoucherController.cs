using Microsoft.AspNetCore.Mvc;

namespace ShopeeVoucherWeb.Controllers
{
    public class VoucherController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
