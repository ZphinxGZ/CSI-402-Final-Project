using System.Diagnostics;
using CSI_402_Final_Project.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CSI_402_Final_Project.Controllers;

public class HomeController : Controller
{
    private readonly Projectcsi402dbContext _db;

    public HomeController(Projectcsi402dbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public IActionResult Index()
    {
        var products = _db.Products
            .Include(p => p.Category)
            .Include(p => p.Promotionproducts)
            .ThenInclude(pp => pp.Promotion)
            .OrderByDescending(p => p.CreatedAt)
            .Take(12)
            .ToList();

        var now = DateTime.Now;
        var discountMap = new Dictionary<int, decimal>();
        foreach (var p in products)
        {
            var originalPrice = p.Price ?? 0;
            var activePromo = p.Promotionproducts?
                .Select(pp => pp.Promotion)
                .FirstOrDefault(promo =>
                    promo != null &&
                    promo.IsActive == true &&
                    promo.StartDate <= now &&
                    promo.EndDate >= now);

            if (activePromo != null)
            {
                if (activePromo.DiscountType == "percentage" && activePromo.DiscountValue.HasValue)
                    discountMap[p.Id] = Math.Round(originalPrice * (1 - activePromo.DiscountValue.Value / 100), 2);
                else if (activePromo.DiscountType == "fixed" && activePromo.DiscountValue.HasValue)
                {
                    var r = originalPrice - activePromo.DiscountValue.Value;
                    discountMap[p.Id] = r > 0 ? Math.Round(r, 2) : 0;
                }
                else
                    discountMap[p.Id] = originalPrice;
            }
            else
            {
                discountMap[p.Id] = originalPrice;
            }
        }
        ViewBag.DiscountMap = discountMap;

        return View(products);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    // [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    // public IActionResult Error()
    // {
    //     return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    // }
}
