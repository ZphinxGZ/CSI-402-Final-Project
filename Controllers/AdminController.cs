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
        ViewBag.TotalUsers = _db.Users.Count();
        ViewBag.TotalProducts = _db.Products.Count();
        ViewBag.TotalOrders = _db.Orders.Count();
        ViewBag.PendingOrders = _db.Orders.Count(o => o.Status == "Pending");
        ViewBag.TotalPromotions = _db.Promotions.Count(p => p.IsActive == true);
        return View();
    }

    // ===== ORDER MANAGEMENT =====

    [HttpGet]
    public IActionResult Orders(string? status = null)
    {
        var query = _db.Orders
            .Include(o => o.User)
            .Include(o => o.Orderitems)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(o => o.Status == status);
            ViewData["statusFilter"] = status;
        }

        var orders = query.OrderByDescending(o => o.CreatedAt).ToList();
        return View(orders);
    }

    [HttpGet]
    public IActionResult OrderDetails(int id)
    {
        var order = _db.Orders
            .Include(o => o.User)
            .Include(o => o.Orderitems)
            .ThenInclude(oi => oi.Product)
            .FirstOrDefault(o => o.Id == id);

        if (order == null) return NotFound();

        var vm = new OrderViewModel
        {
            Id = order.Id,
            Status = order.Status,
            TotalPrice = order.TotalPrice,
            Discount = order.Discount,
            FinalPrice = order.FinalPrice,
            CreatedAt = order.CreatedAt,
            UserName = (order.User?.Name ?? "") + " " + (order.User?.Lastname ?? ""),
            UserEmail = order.User?.Email,
            Items = order.Orderitems.Select(oi => new OrderItemViewModel
            {
                ProductName = oi.Product?.Name ?? "Unknown",
                ImageUrl = oi.Product?.ImageUrl,
                Quantity = oi.Quantity ?? 0,
                Price = oi.Price ?? 0
            }).ToList()
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateOrderStatus(int id, string status)
    {
        var order = _db.Orders.Find(id);
        if (order == null) return NotFound();

        order.Status = status;
        _db.SaveChanges();

        TempData["SwalIcon"] = "success";
        TempData["SwalTitle"] = "Status Updated";
        TempData["SwalMessage"] = $"Order #{id} status changed to {status}.";

        return RedirectToAction(nameof(OrderDetails), new { id });
    }

    // ===== PROMOTION MANAGEMENT =====

    [HttpGet]
    public IActionResult Promotions()
    {
        var promotions = _db.Promotions
            .Include(p => p.Promotionproducts)
            .ThenInclude(pp => pp.Product)
            .OrderByDescending(p => p.IsActive)
            .ThenByDescending(p => p.EndDate)
            .ToList();
        return View(promotions);
    }

    [HttpGet]
    public IActionResult CreatePromotion()
    {
        ViewBag.Products = _db.Products.OrderBy(p => p.Name).ToList();
        return View(new PromotionViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CreatePromotion(PromotionViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Products = _db.Products.OrderBy(p => p.Name).ToList();
            return View(vm);
        }

        var promotion = new Promotion
        {
            Name = vm.Name,
            Description = vm.Description,
            DiscountType = vm.DiscountType,
            DiscountValue = vm.DiscountValue,
            MinOrderAmount = vm.MinOrderAmount,
            StartDate = vm.StartDate,
            EndDate = vm.EndDate,
            IsActive = vm.IsActive
        };

        _db.Promotions.Add(promotion);
        _db.SaveChanges();

        // Add selected products
        foreach (var productId in vm.SelectedProductIds)
        {
            _db.Promotionproducts.Add(new Promotionproduct
            {
                PromotionId = promotion.Id,
                ProductId = productId
            });
        }
        _db.SaveChanges();

        TempData["SwalIcon"] = "success";
        TempData["SwalTitle"] = "Promotion Created";
        TempData["SwalMessage"] = "Promotion has been created successfully.";

        return RedirectToAction(nameof(Promotions));
    }

    [HttpGet]
    public IActionResult EditPromotion(int id)
    {
        var promotion = _db.Promotions
            .Include(p => p.Promotionproducts)
            .FirstOrDefault(p => p.Id == id);

        if (promotion == null) return NotFound();

        var vm = new PromotionViewModel
        {
            Id = promotion.Id,
            Name = promotion.Name ?? "",
            Description = promotion.Description,
            DiscountType = promotion.DiscountType ?? "percentage",
            DiscountValue = promotion.DiscountValue ?? 0,
            MinOrderAmount = promotion.MinOrderAmount,
            StartDate = promotion.StartDate ?? DateTime.Now,
            EndDate = promotion.EndDate ?? DateTime.Now.AddDays(30),
            IsActive = promotion.IsActive ?? true,
            SelectedProductIds = promotion.Promotionproducts.Select(pp => pp.ProductId ?? 0).Where(id => id > 0).ToList()
        };

        ViewBag.Products = _db.Products.OrderBy(p => p.Name).ToList();
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult EditPromotion(int id, PromotionViewModel vm)
    {
        if (id != vm.Id) return BadRequest();

        if (!ModelState.IsValid)
        {
            ViewBag.Products = _db.Products.OrderBy(p => p.Name).ToList();
            return View(vm);
        }

        var promotion = _db.Promotions
            .Include(p => p.Promotionproducts)
            .FirstOrDefault(p => p.Id == id);

        if (promotion == null) return NotFound();

        promotion.Name = vm.Name;
        promotion.Description = vm.Description;
        promotion.DiscountType = vm.DiscountType;
        promotion.DiscountValue = vm.DiscountValue;
        promotion.MinOrderAmount = vm.MinOrderAmount;
        promotion.StartDate = vm.StartDate;
        promotion.EndDate = vm.EndDate;
        promotion.IsActive = vm.IsActive;

        // Update product associations
        _db.Promotionproducts.RemoveRange(promotion.Promotionproducts);
        foreach (var productId in vm.SelectedProductIds)
        {
            _db.Promotionproducts.Add(new Promotionproduct
            {
                PromotionId = promotion.Id,
                ProductId = productId
            });
        }

        _db.SaveChanges();

        TempData["SwalIcon"] = "success";
        TempData["SwalTitle"] = "Promotion Updated";
        TempData["SwalMessage"] = "Promotion has been updated successfully.";

        return RedirectToAction(nameof(Promotions));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeletePromotion(int id)
    {
        var promotion = _db.Promotions
            .Include(p => p.Promotionproducts)
            .FirstOrDefault(p => p.Id == id);

        if (promotion == null) return NotFound();

        _db.Promotionproducts.RemoveRange(promotion.Promotionproducts);
        _db.Promotions.Remove(promotion);
        _db.SaveChanges();

        TempData["SwalIcon"] = "success";
        TempData["SwalTitle"] = "Promotion Deleted";
        TempData["SwalMessage"] = "Promotion has been deleted.";

        return RedirectToAction(nameof(Promotions));
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
