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

        // Sort by price
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
            .FirstOrDefault(p => p.Id == id);

        if (product == null)
        {
            return NotFound();
        }

        return View(product);
    }
}
