using Microsoft.AspNetCore.Mvc;

namespace OrderService.Controllers
{
    public class VendorController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
