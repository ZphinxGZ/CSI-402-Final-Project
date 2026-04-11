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

    // หน้าแรก - แสดงโปรโมชั่น + สินค้าลดราคา + สินค้าล่าสุด
    [HttpGet]
    public IActionResult Index()
    {
        var now = DateTime.Now;

        // ดึงโปรโมชั่นที่ Active อยู่
        var activePromotions = _db.Promotions
            .Include(p => p.Promotionproducts)
            .ThenInclude(pp => pp.Product)
            .Where(p => p.IsActive == true && p.StartDate <= now && p.EndDate >= now)
            .OrderByDescending(p => p.DiscountValue)
            .ToList();

        ViewBag.ActivePromotions = activePromotions;

        // ดึงสินค้า 12 ชิ้นล่าสุด
        var products = _db.Products
            .Include(p => p.Category)
            .Include(p => p.Promotionproducts)
            .ThenInclude(pp => pp.Promotion)
            .OrderByDescending(p => p.CreatedAt)
            .Take(12)
            .ToList();

        // คำนวณราคาหลังส่วนลด
        var discountMap = new Dictionary<int, decimal>();

        foreach (var p in products)
        {
            decimal originalPrice = p.Price ?? 0;
            decimal finalPrice = originalPrice;

            // วนหาโปรโมชั่นที่ Active
            if (p.Promotionproducts != null)
            {
                foreach (var pp in p.Promotionproducts)
                {
                    if (pp.Promotion != null
                        && pp.Promotion.IsActive == true
                        && pp.Promotion.StartDate <= now
                        && pp.Promotion.EndDate >= now)
                    {
                        // คำนวณส่วนลดเป็น % (จำกัด 0-100)
                        if (pp.Promotion.DiscountValue != null && pp.Promotion.DiscountValue > 0)
                        {
                            decimal discountPercent = pp.Promotion.DiscountValue.Value;
                            if (discountPercent > 100) discountPercent = 100;
                            decimal discountAmount = originalPrice * discountPercent / 100;
                            finalPrice = Math.Round(originalPrice - discountAmount, 2);
                            if (finalPrice < 0) finalPrice = 0;
                        }
                        break;
                    }
                }
            }

            discountMap[p.Id] = finalPrice;
        }

        ViewBag.DiscountMap = discountMap;
        return View(products);
    }

    public IActionResult Privacy()
    {
        return View();
    }
}
