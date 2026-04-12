using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CSI_402_Final_Project.Models;
using CSI_402_Final_Project.ViewModel;
using CSI_402_Final_Project.Security;

namespace CSI_402_Final_Project.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly Projectcsi402dbContext _db;

    public AdminController(Projectcsi402dbContext db)
    {
        _db = db;
    }

    // หน้า Dashboard
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

    // ===== จัดการคำสั่งซื้อ =====

    // แสดงรายการคำสั่งซื้อทั้งหมด
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

        var orders = query.OrderBy(o => o.Id).ToList();
        return View(orders);
    }

    // แสดงรายละเอียดคำสั่งซื้อ
    [HttpGet]
    public IActionResult OrderDetails(int id)
    {
        var order = _db.Orders
            .Include(o => o.User)
            .Include(o => o.Orderitems)
            .ThenInclude(oi => oi.Product)
            .FirstOrDefault(o => o.Id == id);

        if (order == null) return NotFound();

        // สร้าง ViewModel
        var viewModel = new OrderViewModel();
        viewModel.Id = order.Id;
        viewModel.Status = order.Status;
        viewModel.TotalPrice = order.TotalPrice ?? 0;
        viewModel.Discount = order.Discount ?? 0;
        viewModel.FinalPrice = order.FinalPrice ?? 0;
        viewModel.CreatedAt = order.CreatedAt;

        if (order.User != null)
        {
            viewModel.UserName = (order.User.Name ?? "") + " " + (order.User.Lastname ?? "");
            viewModel.UserEmail = order.User.Email ?? "";
        }

        // วนลูปสร้าง OrderItemViewModel
        foreach (var oi in order.Orderitems)
        {
            var item = new OrderItemViewModel();
            item.ProductName = oi.Product != null ? oi.Product.Name ?? "Unknown" : "Unknown";
            item.ImageUrl = oi.Product != null ? oi.Product.ImageUrl : null;
            item.Quantity = oi.Quantity ?? 0;
            item.Price = oi.Price ?? 0;
            item.Total = item.Price * item.Quantity;
            viewModel.Items.Add(item);
        }

        return View(viewModel);
    }

    // อัพเดทสถานะคำสั่งซื้อ
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
        TempData["SwalMessage"] = "Order #" + id + " status changed to " + status + ".";

        return RedirectToAction("OrderDetails", new { id = id });
    }

    // ===== จัดการโปรโมชั่น =====

    // แสดงรายการโปรโมชั่นทั้งหมด
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

    // หน้าสร้างโปรโมชั่นใหม่
    [HttpGet]
    public IActionResult CreatePromotion()
    {
        ViewBag.Products = _db.Products.Include(p => p.Category).OrderBy(p => p.Name).ToList();
        ViewBag.Categories = _db.Categories.OrderBy(c => c.Name).ToList();
        return View(new PromotionViewModel());
    }

    // บันทึกโปรโมชั่นใหม่
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CreatePromotion(PromotionViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Products = _db.Products.Include(p => p.Category).OrderBy(p => p.Name).ToList();
            ViewBag.Categories = _db.Categories.OrderBy(c => c.Name).ToList();
            return View(vm);
        }

        // จำกัดส่วนลดไม่เกิน 0-100%
        if (vm.DiscountValue < 0) vm.DiscountValue = 0;
        if (vm.DiscountValue > 100) vm.DiscountValue = 100;

        // สร้าง Promotion
        var promotion = new Promotion();
        promotion.Name = vm.Name;
        promotion.Description = vm.Description;
        promotion.DiscountType = vm.IsGlobal ? "global" : "percentage";
        promotion.DiscountValue = vm.DiscountValue;
        promotion.StartDate = vm.StartDate;
        promotion.EndDate = vm.EndDate;
        promotion.IsActive = vm.IsActive;

        _db.Promotions.Add(promotion);
        _db.SaveChanges();

        // ถ้าไม่ใช่ Global ให้เพิ่มสินค้าที่เลือก
        if (!vm.IsGlobal)
        {
            foreach (var productId in vm.SelectedProductIds)
            {
                var pp = new Promotionproduct();
                pp.PromotionId = promotion.Id;
                pp.ProductId = productId;
                _db.Promotionproducts.Add(pp);
            }
            _db.SaveChanges();
        }

        TempData["SwalIcon"] = "success";
        TempData["SwalTitle"] = "Promotion Created";
        TempData["SwalMessage"] = "Promotion has been created successfully.";

        return RedirectToAction("Promotions");
    }

    // หน้าแก้ไขโปรโมชั่น
    [HttpGet]
    public IActionResult EditPromotion(int id)
    {
        var promotion = _db.Promotions
            .Include(p => p.Promotionproducts)
            .FirstOrDefault(p => p.Id == id);

        if (promotion == null) return NotFound();

        var vm = new PromotionViewModel();
        vm.Id = promotion.Id;
        vm.Name = promotion.Name ?? "";
        vm.Description = promotion.Description;
        vm.DiscountValue = promotion.DiscountValue ?? 0;
        vm.StartDate = promotion.StartDate ?? DateTime.Now;
        vm.EndDate = promotion.EndDate ?? DateTime.Now.AddDays(30);
        vm.IsActive = promotion.IsActive ?? true;
        vm.IsGlobal = promotion.DiscountType == "global";

        // ดึง ProductId ที่เลือกไว้
        foreach (var pp in promotion.Promotionproducts)
        {
            if (pp.ProductId != null && pp.ProductId > 0)
            {
                vm.SelectedProductIds.Add(pp.ProductId.Value);
            }
        }

        ViewBag.Products = _db.Products.Include(p => p.Category).OrderBy(p => p.Name).ToList();
        ViewBag.Categories = _db.Categories.OrderBy(c => c.Name).ToList();
        return View(vm);
    }

    // บันทึกการแก้ไขโปรโมชั่น
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult EditPromotion(int id, PromotionViewModel vm)
    {
        if (id != vm.Id) return BadRequest();

        if (!ModelState.IsValid)
        {
            ViewBag.Products = _db.Products.Include(p => p.Category).OrderBy(p => p.Name).ToList();
            ViewBag.Categories = _db.Categories.OrderBy(c => c.Name).ToList();
            return View(vm);
        }

        var promotion = _db.Promotions
            .Include(p => p.Promotionproducts)
            .FirstOrDefault(p => p.Id == id);

        if (promotion == null) return NotFound();

        // จำกัดส่วนลดไม่เกิน 0-100%
        if (vm.DiscountValue < 0) vm.DiscountValue = 0;
        if (vm.DiscountValue > 100) vm.DiscountValue = 100;

        // อัพเดทข้อมูล
        promotion.Name = vm.Name;
        promotion.Description = vm.Description;
        promotion.DiscountType = vm.IsGlobal ? "global" : "percentage";
        promotion.DiscountValue = vm.DiscountValue;
        promotion.StartDate = vm.StartDate;
        promotion.EndDate = vm.EndDate;
        promotion.IsActive = vm.IsActive;

        // ลบสินค้าเก่า แล้วเพิ่มใหม่
        _db.Promotionproducts.RemoveRange(promotion.Promotionproducts);
        if (!vm.IsGlobal)
        {
            foreach (var productId in vm.SelectedProductIds)
            {
                var pp = new Promotionproduct();
                pp.PromotionId = promotion.Id;
                pp.ProductId = productId;
                _db.Promotionproducts.Add(pp);
            }
        }

        _db.SaveChanges();

        TempData["SwalIcon"] = "success";
        TempData["SwalTitle"] = "Promotion Updated";
        TempData["SwalMessage"] = "Promotion has been updated successfully.";

        return RedirectToAction("Promotions");
    }

    // ลบโปรโมชั่น
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

        return RedirectToAction("Promotions");
    }

    // ===== จัดการผู้ใช้ =====

    // แสดงรายการผู้ใช้ทั้งหมด
    [HttpGet]
    public IActionResult Users()
    {
        var users = _db.Users
            .OrderBy(u => u.Id)
            .ToList();
        return View(users);
    }

    // หน้าแก้ไขข้อมูลผู้ใช้
    [HttpGet]
    public IActionResult EditUser(int id)
    {
        var user = _db.Users.Find(id);
        if (user == null) return NotFound();

        var vm = new AdminUserEditViewModel();
        vm.Id = user.Id;
        vm.Email = user.Email ?? "";
        vm.Name = user.Name ?? "";
        vm.Lastname = user.Lastname ?? "";
        vm.PhoneNumber = user.PhoneNumber;
        vm.Role = user.Role ?? "User";

        return View(vm);
    }

    // บันทึกการแก้ไขข้อมูลผู้ใช้
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult EditUser(int id, AdminUserEditViewModel vm)
    {
        if (id != vm.Id) return BadRequest();
        if (!ModelState.IsValid) return View(vm);

        var user = _db.Users.Find(id);
        if (user == null) return NotFound();

        user.Name = vm.Name;
        user.Lastname = vm.Lastname;
        user.PhoneNumber = vm.PhoneNumber;
        user.Role = vm.Role;

        _db.SaveChanges();

        TempData["SwalIcon"] = "success";
        TempData["SwalTitle"] = "Updated";
        TempData["SwalMessage"] = "User information has been updated.";

        return RedirectToAction("Users");
    }

    // ===== จัดการสินค้า =====

    // แสดงรายการสินค้าทั้งหมด
    [HttpGet]
    public IActionResult Products()
    {
        var products = _db.Products
            .Include(p => p.Category)
            .OrderBy(p => p.Id)
            .ToList();
        return View(products);
    }

    // หน้าสร้างสินค้าใหม่
    [HttpGet]
    public IActionResult CreateProduct()
    {
        ViewBag.Categories = _db.Categories.OrderBy(c => c.Name).ToList();
        return View();
    }

    // บันทึกสินค้าใหม่
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CreateProduct(ProductCreateViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Categories = _db.Categories.OrderBy(c => c.Name).ToList();
            return View(vm);
        }

        var product = new Product();
        product.Name = vm.Name;
        product.Description = vm.Description;
        product.Price = vm.Price;
        product.CategoryId = vm.CategoryId;
        product.ImageUrl = vm.ImageUrl;
        product.Stock = vm.Stock;
        product.CreatedAt = DateTime.Now;

        _db.Products.Add(product);
        _db.SaveChanges();

        TempData["SwalIcon"] = "success";
        TempData["SwalTitle"] = "Product Created";
        TempData["SwalMessage"] = "Product has been created successfully.";

        return RedirectToAction("Products");
    }

    // หน้าแก้ไขสินค้า
    [HttpGet]
    public IActionResult EditProduct(int id)
    {
        var product = _db.Products.Find(id);
        if (product == null) return NotFound();

        var vm = new ProductCreateViewModel();
        vm.Id = product.Id;
        vm.Name = product.Name ?? "";
        vm.Description = product.Description;
        vm.Price = product.Price ?? 0;
        vm.CategoryId = product.CategoryId ?? 0;
        vm.ImageUrl = product.ImageUrl;
        vm.Stock = product.Stock;

        ViewBag.Categories = _db.Categories.OrderBy(c => c.Name).ToList();
        return View(vm);
    }

    // บันทึกการแก้ไขสินค้า
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult EditProduct(int id, ProductCreateViewModel vm)
    {
        if (id != vm.Id) return BadRequest();

        if (!ModelState.IsValid)
        {
            ViewBag.Categories = _db.Categories.OrderBy(c => c.Name).ToList();
            return View(vm);
        }

        var product = _db.Products.Find(id);
        if (product == null) return NotFound();

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

        return RedirectToAction("Products");
    }

    // ลบสินค้า
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteProduct(int id)
    {
        var product = _db.Products.Find(id);
        if (product == null) return NotFound();

        _db.Products.Remove(product);
        _db.SaveChanges();

        TempData["SwalIcon"] = "success";
        TempData["SwalTitle"] = "Product Deleted";
        TempData["SwalMessage"] = "Product has been deleted successfully.";

        return RedirectToAction("Products");
    }

    // ===== จัดการหมวดหมู่ =====

    // สร้างหมวดหมู่ใหม่ (AJAX)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CreateCategory(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Json(new { success = false, message = "Category name is required." });
        }

        // เช็คซ้ำ
        bool exists = _db.Categories.Any(c => c.Name == name);
        if (exists)
        {
            return Json(new { success = false, message = "Category already exists." });
        }

        var category = new Category();
        category.Name = name;
        _db.Categories.Add(category);
        _db.SaveChanges();

        return Json(new { success = true, id = category.Id, name = category.Name });
    }

    // ===== เพิ่มผู้ใช้ใหม่ =====

    [HttpGet]
    public IActionResult CreateUser()
    {
        return View(new AdminCreateUserViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CreateUser(AdminCreateUserViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        // เช็ค email ซ้ำ
        bool emailExists = _db.Users.Any(u => u.Email == vm.Email);
        if (emailExists)
        {
            ModelState.AddModelError("Email", "Email is already registered.");
            return View(vm);
        }

        var user = new User();
        user.Name = vm.Name;
        user.Lastname = vm.Lastname;
        user.Email = vm.Email;
        user.Password = PasswordHasher.Hash(vm.Password);
        user.PhoneNumber = vm.PhoneNumber;
        user.Role = vm.Role;
        user.CreatedAt = DateTime.UtcNow;

        _db.Users.Add(user);
        _db.SaveChanges();

        TempData["SwalIcon"] = "success";
        TempData["SwalTitle"] = "User Created";
        TempData["SwalMessage"] = "User has been created successfully.";

        return RedirectToAction("Users");
    }
}
