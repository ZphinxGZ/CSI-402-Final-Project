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

    [HttpGet]
    public IActionResult Index()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Challenge();
        }

        var cart = GetOrCreateCart(userId.Value);
        var cartViewModel = new CartViewModel
        {
            CartId = cart.Id,
            Items = cart.Cartitems.Select(item =>
            {
                var originalPrice = item.Product?.Price ?? 0;
                var discountedPrice = GetDiscountedPrice(item.ProductId ?? 0, originalPrice);
                return new CartItemViewModel
                {
                    Id = item.Id,
                    ProductId = item.ProductId ?? 0,
                    ProductName = item.Product?.Name ?? "Unknown",
                    OriginalPrice = originalPrice,
                    Price = discountedPrice,
                    Quantity = item.Quantity ?? 0,
                    ImageUrl = item.Product?.ImageUrl,
                    Stock = item.Product?.Stock ?? 0
                };
            }).ToList()
        };

        return View(cartViewModel);
    }

    [HttpPost]
    public IActionResult AddToCart(int productId, int quantity = 1)
    {
        var userId = GetCurrentUserId();
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
        
        var existingItem = cart.Cartitems.FirstOrDefault(item => item.ProductId == productId);
        if (existingItem != null)
        {
            var newQuantity = existingItem.Quantity + quantity;
            if (product.Stock < newQuantity)
            {
                return Json(new { success = false, message = "Not enough stock available." });
            }
            existingItem.Quantity = newQuantity;
        }
        else
        {
            cart.Cartitems.Add(new Cartitem
            {
                ProductId = productId,
                Quantity = quantity
            });
        }

        _db.SaveChanges();

        return Json(new { 
            success = true, 
            message = "Product added to cart successfully.",
            cartCount = cart.Cartitems.Sum(item => item.Quantity)
        });
    }

    [HttpPost]
    public IActionResult UpdateQuantity(int itemId, int quantity)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Json(new { success = false, message = "Please login to update cart." });
        }

        var cartItem = _db.Cartitems
            .Include(item => item.Product)
            .Include(item => item.Cart)
            .FirstOrDefault(item => item.Id == itemId && item.Cart != null && item.Cart.UserId == userId);

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

    [HttpPost]
    public IActionResult RemoveFromCart(int itemId)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Json(new { success = false, message = "Please login to update cart." });
        }

        var cartItem = _db.Cartitems
            .Include(item => item.Cart)
            .FirstOrDefault(item => item.Id == itemId && item.Cart != null && item.Cart.UserId == userId);

        if (cartItem == null)
        {
            return Json(new { success = false, message = "Cart item not found." });
        }

        _db.Cartitems.Remove(cartItem);
        _db.SaveChanges();

        return Json(new { success = true, message = "Item removed from cart." });
    }

    private decimal GetDiscountedPrice(int productId, decimal originalPrice)
    {
        var now = DateTime.Now;

        var promoProduct = _db.Promotionproducts
            .Include(pp => pp.Promotion)
            .FirstOrDefault(pp =>
                pp.ProductId == productId &&
                pp.Promotion != null &&
                pp.Promotion.IsActive == true &&
                pp.Promotion.StartDate <= now &&
                pp.Promotion.EndDate >= now);

        if (promoProduct?.Promotion == null) return originalPrice;

        var promo = promoProduct.Promotion;

        if (promo.DiscountType == "percentage" && promo.DiscountValue.HasValue)
        {
            return Math.Round(originalPrice * (1 - promo.DiscountValue.Value / 100), 2);
        }
        else if (promo.DiscountType == "fixed" && promo.DiscountValue.HasValue)
        {
            var result = originalPrice - promo.DiscountValue.Value;
            return result > 0 ? Math.Round(result, 2) : 0;
        }

        return originalPrice;
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }
        return null;
    }

    private Cart GetOrCreateCart(int userId)
    {
        var cart = _db.Carts
            .Include(c => c.Cartitems)
            .ThenInclude(item => item.Product)
            .FirstOrDefault(c => c.UserId == userId);

        if (cart == null)
        {
            cart = new Cart { UserId = userId };
            _db.Carts.Add(cart);
            _db.SaveChanges();
            
            cart = _db.Carts
                .Include(c => c.Cartitems)
                .ThenInclude(item => item.Product)
                .FirstOrDefault(c => c.UserId == userId);
        }

        return cart ?? throw new InvalidOperationException("Failed to create or retrieve cart.");
    }
}
