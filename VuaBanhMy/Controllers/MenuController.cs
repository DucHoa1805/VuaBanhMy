using Microsoft.AspNetCore.Mvc;
using VuaBanhMy.Services;

namespace VuaBanhMy.Controllers
{
    // Trang thực đơn — ai cũng xem được, không cần đăng nhập
    public class MenuController(IProductService productService) : Controller
    {
        public async Task<IActionResult> Index()
        {
            var menu = await productService.GetMenuAsync();
            return View(menu);
        }
    }
}
