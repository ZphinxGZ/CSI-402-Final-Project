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
    public IActionResult Users()
    {
        var users = _db.Users
            .OrderByDescending(u => u.CreatedAt)
            .ToList();
        return View(users);
    }

    [HttpGet]
    public IActionResult EditUser(int id)
    {
        var user = _db.Users.Find(id);
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
    public IActionResult EditUser(int id, AdminUserEditViewModel vm)
    {
        if (id != vm.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        var user = _db.Users.Find(id);
        if (user == null)
        {
            return NotFound();
        }

        user.Name = vm.Name;
        user.Lastname = vm.Lastname;
        user.PhoneNumber = vm.PhoneNumber;
        user.Role = vm.Role;

        _db.SaveChanges();

        TempData["SwalIcon"] = "success";
        TempData["SwalTitle"] = "Updated";
        TempData["SwalMessage"] = "User information has been updated.";

        return RedirectToAction(nameof(Users));
    }

    [HttpGet]
    public IActionResult Products()
    {
        var products = _db.Products
            .Include(p => p.Category)
            .OrderByDescending(p => p.CreatedAt)
            .ToList();
        return View(products);
    }

    [HttpGet]
    public IActionResult CreateProduct()
    {
        var categories = _db.Categories
            .OrderBy(c => c.Name)
            .ToList();
        
        ViewBag.Categories = categories;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CreateProduct(ProductCreateViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            var categories = _db.Categories
                .OrderBy(c => c.Name)
                .ToList();
            ViewBag.Categories = categories;
            return View(vm);
        }

        var product = new Product
        {
            Name = vm.Name,
            Description = vm.Description,
            Price = vm.Price,
            CategoryId = vm.CategoryId,
            ImageUrl = vm.ImageUrl,
            Stock = vm.Stock,
            CreatedAt = DateTime.Now
        };

        _db.Products.Add(product);
        _db.SaveChanges();

        TempData["SwalIcon"] = "success";
        TempData["SwalTitle"] = "Product Created";
        TempData["SwalMessage"] = "Product has been created successfully.";

        return RedirectToAction(nameof(Products));
    }

    [HttpGet]
    public IActionResult EditProduct(int id)
    {
        var product = _db.Products.Find(id);
        if (product == null)
        {
            return NotFound();
        }

        var vm = new ProductCreateViewModel
        {
            Id = product.Id,
            Name = product.Name ?? "",
            Description = product.Description,
            Price = product.Price ?? 0,
            CategoryId = product.CategoryId ?? 0,
            ImageUrl = product.ImageUrl,
            Stock = product.Stock
        };

        var categories = _db.Categories
            .OrderBy(c => c.Name)
            .ToList();
        ViewBag.Categories = categories;

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult EditProduct(int id, ProductCreateViewModel vm)
    {
        if (id != vm.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            var categories = _db.Categories
                .OrderBy(c => c.Name)
                .ToList();
            ViewBag.Categories = categories;
            return View(vm);
        }

        var product = _db.Products.Find(id);
        if (product == null)
        {
            return NotFound();
        }

        product.Name = vm.Name;
        product.Description = vm.Description;
        product.Price = vm.Price;
        product.CategoryId = vm.CategoryId;
        product.ImageUrl = vm.ImageUrl;
        product.Stock = vm.Stock;

        _db.SaveChanges();

        TempData["SwalIcon"] = "success";
        TempData["SwalTitle"] = "Product Updated";
        TempData["SwalMessage"] = "Product has been updated successfully.";

        return RedirectToAction(nameof(Products));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteProduct(int id)
    {
        var product = _db.Products.Find(id);
        if (product == null)
        {
            return NotFound();
        }

        _db.Products.Remove(product);
        _db.SaveChanges();

        TempData["SwalIcon"] = "success";
        TempData["SwalTitle"] = "Product Deleted";
        TempData["SwalMessage"] = "Product has been deleted successfully.";

        return RedirectToAction(nameof(Products));
    }
}
