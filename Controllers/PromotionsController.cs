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
    public async Task<IActionResult> Index()
    {
        var promotions = await _db.Promotions
            .OrderByDescending(p => p.IsActive)
            .ThenBy(p => p.EndDate)
            .ToListAsync();

        return View(promotions);
    }
}
