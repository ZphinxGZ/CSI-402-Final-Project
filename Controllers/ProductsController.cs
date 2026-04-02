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
    public IActionResult Index(string? q = null)
    {
        var query = _db.Products
            .Include(p => p.Category)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            ViewData["q"] = q;
            query = query.Where(p => (p.Name ?? "").Contains(q) || (p.Category != null && (p.Category.Name ?? "").Contains(q)));
        }

        var products = query
            .OrderByDescending(p => p.CreatedAt)
            .ToList();

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
