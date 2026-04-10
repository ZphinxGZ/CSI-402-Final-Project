using CSI_402_Final_Project.Models;
using CSI_402_Final_Project.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CSI_402_Final_Project.Controllers;

[Authorize]
public class OrderController : Controller
{
    private readonly Projectcsi402dbContext _db;

    public OrderController(Projectcsi402dbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public IActionResult Checkout()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Challenge();

        var cart = _db.Carts
            .Include(c => c.Cartitems)
            .ThenInclude(ci => ci.Product)
            .FirstOrDefault(c => c.UserId == userId);

        if (cart == null || !cart.Cartitems.Any())
        {
            TempData["SwalIcon"] = "warning";
            TempData["SwalTitle"] = "Cart Empty";
            TempData["SwalMessage"] = "Please add items to your cart before checkout.";
            return RedirectToAction("Index", "Cart");
        }

        var addresses = _db.Shippingaddresses
            .Where(a => a.UserId == userId)
            .ToList();

        var items = new List<CheckoutItemViewModel>();
        decimal subTotal = 0;
        decimal discount = 0;

        foreach (var ci in cart.Cartitems)
        {
            var product = ci.Product;
            if (product == null) continue;

            var originalPrice = product.Price ?? 0;
            var finalPrice = GetDiscountedPrice(product.Id, originalPrice);
            var itemDiscount = (originalPrice - finalPrice) * (ci.Quantity ?? 0);

            items.Add(new CheckoutItemViewModel
            {
                ProductId = product.Id,
                ProductName = product.Name ?? "Unknown",
                ImageUrl = product.ImageUrl,
                Quantity = ci.Quantity ?? 0,
                OriginalPrice = originalPrice,
                FinalPrice = finalPrice
            });

            subTotal += originalPrice * (ci.Quantity ?? 0);
            discount += itemDiscount;
        }

        var vm = new CheckoutViewModel
        {
            Items = items,
            Addresses = addresses,
            SubTotal = subTotal,
            Discount = discount,
            Total = subTotal - discount
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult PlaceOrder(int? selectedAddressId)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Challenge();

        var cart = _db.Carts
            .Include(c => c.Cartitems)
            .ThenInclude(ci => ci.Product)
            .FirstOrDefault(c => c.UserId == userId);

        if (cart == null || !cart.Cartitems.Any())
        {
            TempData["SwalIcon"] = "error";
            TempData["SwalTitle"] = "Error";
            TempData["SwalMessage"] = "Your cart is empty.";
            return RedirectToAction("Index", "Cart");
        }

        decimal totalPrice = 0;
        decimal totalDiscount = 0;

        var order = new Order
        {
            UserId = userId,
            Status = "Pending",
            CreatedAt = DateTime.Now
        };

        _db.Orders.Add(order);
        _db.SaveChanges();

        foreach (var ci in cart.Cartitems)
        {
            var product = ci.Product;
            if (product == null) continue;

            var originalPrice = product.Price ?? 0;
            var finalPrice = GetDiscountedPrice(product.Id, originalPrice);
            var qty = ci.Quantity ?? 0;

            var orderItem = new Orderitem
            {
                OrderId = order.Id,
                ProductId = product.Id,
                Quantity = qty,
                Price = finalPrice
            };

            _db.Orderitems.Add(orderItem);

            totalPrice += originalPrice * qty;
            totalDiscount += (originalPrice - finalPrice) * qty;

            // Reduce stock
            if (product.Stock.HasValue)
            {
                product.Stock -= qty;
                if (product.Stock < 0) product.Stock = 0;
            }
        }

        order.TotalPrice = totalPrice;
        order.Discount = totalDiscount;
        order.FinalPrice = totalPrice - totalDiscount;

        // Clear cart
        _db.Cartitems.RemoveRange(cart.Cartitems);
        _db.SaveChanges();

        return RedirectToAction("Confirmation", new { id = order.Id });
    }

    [HttpGet]
    public IActionResult Confirmation(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Challenge();

        var order = _db.Orders
            .Include(o => o.Orderitems)
            .ThenInclude(oi => oi.Product)
            .FirstOrDefault(o => o.Id == id && o.UserId == userId);

        if (order == null) return NotFound();

        var vm = MapOrderToViewModel(order);
        return View(vm);
    }

    [HttpGet]
    public IActionResult MyOrders()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Challenge();

        var orders = _db.Orders
            .Include(o => o.Orderitems)
            .ThenInclude(oi => oi.Product)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToList();

        var vm = orders.Select(o => MapOrderToViewModel(o)).ToList();
        return View(vm);
    }

    [HttpGet]
    public IActionResult Details(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Challenge();

        var order = _db.Orders
            .Include(o => o.Orderitems)
            .ThenInclude(oi => oi.Product)
            .FirstOrDefault(o => o.Id == id && o.UserId == userId);

        if (order == null) return NotFound();

        var vm = MapOrderToViewModel(order);
        return View(vm);
    }

    // === Helper Methods ===

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

    private OrderViewModel MapOrderToViewModel(Order order)
    {
        return new OrderViewModel
        {
            Id = order.Id,
            Status = order.Status,
            TotalPrice = order.TotalPrice,
            Discount = order.Discount,
            FinalPrice = order.FinalPrice,
            CreatedAt = order.CreatedAt,
            UserName = order.User?.Name,
            UserEmail = order.User?.Email,
            Items = order.Orderitems.Select(oi => new OrderItemViewModel
            {
                ProductName = oi.Product?.Name ?? "Unknown",
                ImageUrl = oi.Product?.ImageUrl,
                Quantity = oi.Quantity ?? 0,
                Price = oi.Price ?? 0
            }).ToList()
        };
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
}
