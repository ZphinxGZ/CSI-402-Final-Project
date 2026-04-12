using CSI_402_Final_Project.Models;
using CSI_402_Final_Project.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CSI_402_Final_Project.Controllers;

[Authorize]
public class CartController : Controller
{
    private readonly Projectcsi402dbContext _db;

    public CartController(Projectcsi402dbContext db)
    {
        _db = db;
    }

    // แสดงหน้าตะกร้าสินค้า
    [HttpGet]
    public IActionResult Index()
    {
        int? userId = GetCurrentUserId();
        if (userId == null)
        {
            return Challenge();
        }

        var cart = GetOrCreateCart(userId.Value);

        // สร้าง ViewModel สำหรับแสดงผล
        var viewModel = new CartViewModel();
        viewModel.CartId = cart.Id;

        // วนลูปสินค้าในตะกร้า
        foreach (var item in cart.Cartitems)
        {
            if (item.Product == null) continue;

            decimal originalPrice = item.Product.Price ?? 0;
            decimal discountedPrice = GetDiscountedPrice(item.ProductId ?? 0, originalPrice);
            int qty = item.Quantity ?? 0;

            var cartItem = new CartItemViewModel();
            cartItem.Id = item.Id;
            cartItem.ProductId = item.ProductId ?? 0;
            cartItem.ProductName = item.Product.Name ?? "Unknown";
            cartItem.OriginalPrice = originalPrice;
            cartItem.Price = discountedPrice;
            cartItem.Quantity = qty;
            cartItem.TotalPrice = discountedPrice * qty;
            cartItem.HasDiscount = discountedPrice < originalPrice;
            cartItem.ImageUrl = item.Product.ImageUrl;
            cartItem.Stock = item.Product.Stock ?? 0;

            viewModel.Items.Add(cartItem);
            viewModel.TotalPrice += cartItem.TotalPrice;
            viewModel.TotalItems += qty;
        }

        return View(viewModel);
    }

    // เพิ่มสินค้าลงตะกร้า (AJAX)
    [HttpPost]
    public IActionResult AddToCart(int productId, int quantity = 1)
    {
        int? userId = GetCurrentUserId();
        if (userId == null)
        {
            return Json(new { success = false, message = "Please login to add items to cart." });
        }

        var product = _db.Products.Find(productId);
        if (product == null)
        {
            return Json(new { success = false, message = "Product not found." });
        }

        if (product.Stock < quantity)
        {
            return Json(new { success = false, message = "Not enough stock available." });
        }

        var cart = GetOrCreateCart(userId.Value);

        // เช็คว่ามีสินค้าชิ้นนี้ในตะกร้าอยู่แล้วหรือไม่
        Cartitem existingItem = null;
        foreach (var item in cart.Cartitems)
        {
            if (item.ProductId == productId)
            {
                existingItem = item;
                break;
            }
        }

        if (existingItem != null)
        {
            // ถ้ามีอยู่แล้ว เพิ่มจำนวน
            int newQuantity = (existingItem.Quantity ?? 0) + quantity;
            if (product.Stock < newQuantity)
            {
                return Json(new { success = false, message = "Not enough stock available." });
            }
            existingItem.Quantity = newQuantity;
        }
        else
        {
            // ถ้ายังไม่มี เพิ่มใหม่
            var newItem = new Cartitem();
            newItem.ProductId = productId;
            newItem.Quantity = quantity;
            cart.Cartitems.Add(newItem);
        }

        _db.SaveChanges();

        // นับจำนวนสินค้าทั้งหมดในตะกร้า
        int cartCount = 0;
        foreach (var item in cart.Cartitems)
        {
            cartCount += item.Quantity ?? 0;
        }

        return Json(new { success = true, message = "Product added to cart successfully.", cartCount = cartCount });
    }

    // อัพเดทจำนวนสินค้า (AJAX)
    [HttpPost]
    public IActionResult UpdateQuantity(int itemId, int quantity)
    {
        int? userId = GetCurrentUserId();
        if (userId == null)
        {
            return Json(new { success = false, message = "Please login to update cart." });
        }

        var cartItem = _db.Cartitems
            .Include(ci => ci.Product)
            .Include(ci => ci.Cart)
            .FirstOrDefault(ci => ci.Id == itemId && ci.Cart != null && ci.Cart.UserId == userId);

        if (cartItem == null)
        {
            return Json(new { success = false, message = "Cart item not found." });
        }

        if (quantity <= 0)
        {
            _db.Cartitems.Remove(cartItem);
        }
        else
        {
            if (cartItem.Product != null && cartItem.Product.Stock < quantity)
            {
                return Json(new { success = false, message = "Not enough stock available." });
            }
            cartItem.Quantity = quantity;
        }

        _db.SaveChanges();
        return Json(new { success = true, message = "Cart updated successfully." });
    }

    // ลบสินค้าออกจากตะกร้า (AJAX)
    [HttpPost]
    public IActionResult RemoveFromCart(int itemId)
    {
        int? userId = GetCurrentUserId();
        if (userId == null)
        {
            return Json(new { success = false, message = "Please login to update cart." });
        }

        var cartItem = _db.Cartitems
            .Include(ci => ci.Cart)
            .FirstOrDefault(ci => ci.Id == itemId && ci.Cart != null && ci.Cart.UserId == userId);

        if (cartItem == null)
        {
            return Json(new { success = false, message = "Cart item not found." });
        }

        _db.Cartitems.Remove(cartItem);
        _db.SaveChanges();

        return Json(new { success = true, message = "Item removed from cart." });
    }

    // คำนวณราคาหลังส่วนลด (ลดซ้อน: โปรสินค้า → โปรเทศกาล)
    private decimal GetDiscountedPrice(int productId, decimal originalPrice)
    {
        var now = DateTime.Now;
        decimal price = originalPrice;

        // ขั้นตอน 2: ลดจากโปรเฉพาะสินค้า (ถ้ามี)
        var promoProduct = _db.Promotionproducts
            .Include(pp => pp.Promotion)
            .FirstOrDefault(pp =>
                pp.ProductId == productId &&
                pp.Promotion != null &&
                pp.Promotion.IsActive == true &&
                pp.Promotion.DiscountType == "percentage" &&
                pp.Promotion.StartDate <= now &&
                pp.Promotion.EndDate >= now);

        if (promoProduct != null && promoProduct.Promotion != null)
        {
            decimal pct = promoProduct.Promotion.DiscountValue ?? 0;
            if (pct > 100) pct = 100;
            if (pct > 0)
            {
                price = Math.Round(price - (price * pct / 100), 2);
                if (price < 0) price = 0;
            }
        }

        // ขั้นตอน 3: ลดจากโปรเทศกาล Global (ถ้ามี)
        var globalPromo = _db.Promotions
            .FirstOrDefault(p =>
                p.IsActive == true &&
                p.DiscountType == "global" &&
                p.StartDate <= now &&
                p.EndDate >= now);

        if (globalPromo != null && globalPromo.DiscountValue != null && globalPromo.DiscountValue > 0)
        {
            decimal gPct = globalPromo.DiscountValue.Value;
            if (gPct > 100) gPct = 100;
            price = Math.Round(price - (price * gPct / 100), 2);
            if (price < 0) price = 0;
        }

        return price;
    }

    // ดึง UserId จาก Cookie
    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim != null && int.TryParse(claim.Value, out int userId))
        {
            return userId;
        }
        return null;
    }

    // ดึงตะกร้าของ User หรือสร้างใหม่ถ้ายังไม่มี
    private Cart GetOrCreateCart(int userId)
    {
        var cart = _db.Carts
            .Include(c => c.Cartitems)
            .ThenInclude(ci => ci.Product)
            .FirstOrDefault(c => c.UserId == userId);

        if (cart == null)
        {
            cart = new Cart();
            cart.UserId = userId;
            _db.Carts.Add(cart);
            _db.SaveChanges();

            // โหลดข้อมูลตะกร้าใหม่พร้อม Product
            cart = _db.Carts
                .Include(c => c.Cartitems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefault(c => c.UserId == userId);
        }

        return cart;
    }
}
