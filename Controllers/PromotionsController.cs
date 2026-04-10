using CSI_402_Final_Project.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CSI_402_Final_Project.Controllers;

public class PromotionsController : Controller
{
    private readonly Projectcsi402dbContext _db;

    public PromotionsController(Projectcsi402dbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public IActionResult Index()
    {
        var promotions = _db.Promotions
            .Include(p => p.Promotionproducts)
            .ThenInclude(pp => pp.Product)
            .OrderByDescending(p => p.IsActive)
            .ThenBy(p => p.EndDate)
            .ToList();

        return View(promotions);
    }
}
