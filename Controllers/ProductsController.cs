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

    [HttpGet]
    public IActionResult Index(string? q = null, int? categoryId = null, string? sortOrder = null)
    {
        var query = _db.Products
            .Include(p => p.Category)
            .Include(p => p.Promotionproducts)
            .ThenInclude(pp => pp.Promotion)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            ViewData["q"] = q;
            query = query.Where(p => (p.Name ?? "").Contains(q) || (p.Category != null && (p.Category.Name ?? "").Contains(q)));
        }

        if (categoryId.HasValue)
        {
            ViewData["categoryId"] = categoryId;
            query = query.Where(p => p.CategoryId == categoryId);
        }

        switch (sortOrder)
        {
            case "price_asc":
                query = query.OrderBy(p => p.Price);
                ViewData["sortOrder"] = "price_asc";
                break;
            case "price_desc":
                query = query.OrderByDescending(p => p.Price);
                ViewData["sortOrder"] = "price_desc";
                break;
            default:
                query = query.OrderByDescending(p => p.CreatedAt);
                break;
        }

        var products = query.ToList();

        // Build discount lookup
        var discountMap = new Dictionary<int, decimal>();
        foreach (var p in products)
        {
            discountMap[p.Id] = GetDiscountedPrice(p);
        }
        ViewBag.DiscountMap = discountMap;

        var categories = _db.Categories
            .OrderBy(c => c.Name)
            .ToList();

        ViewBag.Categories = categories;

        return View(products);
    }

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

    private decimal GetDiscountedPrice(Product product)
    {
        var originalPrice = product.Price ?? 0;
        var now = DateTime.Now;

        var activePromo = product.Promotionproducts?
            .Select(pp => pp.Promotion)
            .FirstOrDefault(promo =>
                promo != null &&
                promo.IsActive == true &&
                promo.StartDate <= now &&
                promo.EndDate >= now);

        if (activePromo == null) return originalPrice;

        if (activePromo.DiscountType == "percentage" && activePromo.DiscountValue.HasValue)
        {
            return Math.Round(originalPrice * (1 - activePromo.DiscountValue.Value / 100), 2);
        }
        else if (activePromo.DiscountType == "fixed" && activePromo.DiscountValue.HasValue)
        {
            var result = originalPrice - activePromo.DiscountValue.Value;
            return result > 0 ? Math.Round(result, 2) : 0;
        }

        return originalPrice;
    }
}
