using Microsoft.AspNetCore.Mvc;

namespace WebNangCao.Controllers
{
    public class BlogController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
