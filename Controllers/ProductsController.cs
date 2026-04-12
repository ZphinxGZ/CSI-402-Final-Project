using CSI_402_Final_Project.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CSI_402_Final_Project.Controllers;

public class ProductsController : Controller
{
    private readonly Projectcsi402dbContext _db;

    public ProductsController(Projectcsi402dbContext db)
    {
        _db = db;
    }

    // แสดงรายการสินค้าทั้งหมด (ค้นหา, กรองหมวดหมู่, เรียงราคา)
    [HttpGet]
    public IActionResult Index(string? q = null, int? categoryId = null, string? sortOrder = null)
    {
        // ดึงสินค้าพร้อมหมวดหมู่และโปรโมชั่น
        var query = _db.Products
            .Include(p => p.Category)
            .Include(p => p.Promotionproducts)
            .ThenInclude(pp => pp.Promotion)
            .AsQueryable();

        // ค้นหาตามชื่อสินค้าหรือหมวดหมู่
        if (!string.IsNullOrWhiteSpace(q))
        {
            ViewData["q"] = q;
            query = query.Where(p => p.Name.Contains(q) || (p.Category != null && p.Category.Name.Contains(q)));
        }

        // กรองตามหมวดหมู่
        if (categoryId != null)
        {
            ViewData["categoryId"] = categoryId;
            query = query.Where(p => p.CategoryId == categoryId);
        }

        // เรียงลำดับ
        if (sortOrder == "price_asc")
        {
            query = query.OrderBy(p => p.Price);
            ViewData["sortOrder"] = "price_asc";
        }
        else if (sortOrder == "price_desc")
        {
            query = query.OrderByDescending(p => p.Price);
            ViewData["sortOrder"] = "price_desc";
        }
        else
        {
            query = query.OrderByDescending(p => p.CreatedAt);
        }

        var products = query.ToList();

        // คำนวณราคาหลังส่วนลดสำหรับแต่ละสินค้า
        var discountMap = new Dictionary<int, decimal>();
        foreach (var p in products)
        {
            discountMap[p.Id] = GetDiscountedPrice(p);
        }
        ViewBag.DiscountMap = discountMap;

        // ดึงหมวดหมู่ทั้งหมด
        ViewBag.Categories = _db.Categories.OrderBy(c => c.Name).ToList();

        return View(products);
    }

    // แสดงรายละเอียดสินค้า
    [HttpGet]
    public IActionResult Details(int id)
    {
        var product = _db.Products
            .Include(p => p.Category)
            .Include(p => p.Promotionproducts)
            .ThenInclude(pp => pp.Promotion)
            .FirstOrDefault(p => p.Id == id);

        if (product == null)
        {
            return NotFound();
        }

        ViewBag.DiscountedPrice = GetDiscountedPrice(product);
        return View(product);
    }

    // คำนวณราคาหลังส่วนลด (เฉพาะโปรสินค้าเท่านั้น ไม่รวมโปรเทศกาล/ลูกค้าใหม่)
    private decimal GetDiscountedPrice(Product product)
    {
        decimal originalPrice = product.Price ?? 0;
        var now = DateTime.Now;

        // หาเฉพาะโปรโมชั่นที่ผูกกับสินค้านี้โดยตรง
        if (product.Promotionproducts != null)
        {
            foreach (var pp in product.Promotionproducts)
            {
                if (pp.Promotion != null
                    && pp.Promotion.IsActive == true
                    && pp.Promotion.DiscountType == "percentage"
                    && pp.Promotion.StartDate <= now
                    && pp.Promotion.EndDate >= now)
                {
                    decimal pct = pp.Promotion.DiscountValue ?? 0;
                    if (pct > 100) pct = 100;
                    if (pct > 0)
                    {
                        decimal result = Math.Round(originalPrice - (originalPrice * pct / 100), 2);
                        if (result < 0) result = 0;
                        return result;
                    }
                    break;
                }
            }
        }

        return originalPrice;
    }
}
