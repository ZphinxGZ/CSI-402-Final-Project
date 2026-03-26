using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CSI_402_Final_Project.Controllers;

[Authorize]
public class CartController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }
}
