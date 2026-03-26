using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CSI_402_Final_Project.Models;
using CSI_402_Final_Project.ViewModel;

namespace CSI_402_Final_Project.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly Projectcsi402dbContext _db;

    public AdminController(Projectcsi402dbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Users()
    {
        var users = await _db.Users
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();
        return View(users);
    }

    [HttpGet]
    public async Task<IActionResult> EditUser(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        var vm = new AdminUserEditViewModel
        {
            Id = user.Id,
            Email = user.Email ?? "",
            Name = user.Name ?? "",
            Lastname = user.Lastname ?? "",
            PhoneNumber = user.PhoneNumber,
            Role = user.Role ?? "User"
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditUser(int id, AdminUserEditViewModel vm)
    {
        if (id != vm.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        var user = await _db.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        user.Name = vm.Name;
        user.Lastname = vm.Lastname;
        user.PhoneNumber = vm.PhoneNumber;
        user.Role = vm.Role;

        await _db.SaveChangesAsync();

        TempData["SwalIcon"] = "success";
        TempData["SwalTitle"] = "Updated";
        TempData["SwalMessage"] = "User information has been updated.";

        return RedirectToAction(nameof(Users));
    }
}
