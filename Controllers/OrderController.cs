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

    // หน้า Checkout - แสดงสรุปก่อนสั่งซื้อ
    [HttpGet]
    public IActionResult Checkout()
    {
        int? userId = GetCurrentUserId();
        if (userId == null) return Challenge();

        // ดึงตะกร้าพร้อมสินค้า
        var cart = _db.Carts
            .Include(c => c.Cartitems)
            .ThenInclude(ci => ci.Product)
            .FirstOrDefault(c => c.UserId == userId);

        if (cart == null || cart.Cartitems.Count == 0)
        {
            TempData["SwalIcon"] = "warning";
            TempData["SwalTitle"] = "Cart Empty";
            TempData["SwalMessage"] = "Please add items to your cart before checkout.";
            return RedirectToAction("Index", "Cart");
        }

        // ดึงที่อยู่จัดส่ง
        var addresses = _db.Shippingaddresses
            .Where(a => a.UserId == userId)
            .ToList();

        // เช็คว่าเป็นลูกค้าใหม่หรือไม่ (ยังไม่เคยสั่งซื้อ)
        bool isNewCustomer = !_db.Orders.Any(o => o.UserId == userId);

        // สร้าง ViewModel
        var viewModel = new CheckoutViewModel();
        viewModel.Addresses = addresses;
        viewModel.IsNewCustomer = isNewCustomer;
        decimal subTotal = 0;
        decimal promoDiscount = 0;

        foreach (var ci in cart.Cartitems)
        {
            if (ci.Product == null) continue;

            decimal originalPrice = ci.Product.Price ?? 0;
            // ดึงราคาหลังลด + ชื่อโปรโมชั่น
            string promoName = "";
            decimal discountPercent = 0;
            decimal finalPrice = GetDiscountedPriceWithInfo(ci.Product.Id, originalPrice, out promoName, out discountPercent);
            int qty = ci.Quantity ?? 0;

            var item = new CheckoutItemViewModel();
            item.ProductId = ci.Product.Id;
            item.ProductName = ci.Product.Name ?? "Unknown";
            item.ImageUrl = ci.Product.ImageUrl;
            item.Quantity = qty;
            item.OriginalPrice = originalPrice;
            item.FinalPrice = finalPrice;
            item.TotalPrice = finalPrice * qty;
            item.HasDiscount = finalPrice < originalPrice;
            item.PromotionName = promoName;
            item.DiscountPercent = discountPercent;

            viewModel.Items.Add(item);

            subTotal += originalPrice * qty;
            promoDiscount += (originalPrice - finalPrice) * qty;
        }

        viewModel.SubTotal = subTotal;
        viewModel.Discount = promoDiscount;

        // ถ้าเป็นลูกค้าใหม่ ลดเพิ่ม 5% จากยอดหลังหักโปร
        decimal afterPromo = subTotal - promoDiscount;
        decimal newCustDiscount = 0;
        if (isNewCustomer)
        {
            newCustDiscount = Math.Round(afterPromo * 5m / 100m, 2);
        }
        viewModel.NewCustomerDiscount = newCustDiscount;
        viewModel.Total = afterPromo - newCustDiscount;

        return View(viewModel);
    }

    // สั่งซื้อ - สร้าง Order จากตะกร้า
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult PlaceOrder(int? selectedAddressId)
    {
        int? userId = GetCurrentUserId();
        if (userId == null) return Challenge();

        var cart = _db.Carts
            .Include(c => c.Cartitems)
            .ThenInclude(ci => ci.Product)
            .FirstOrDefault(c => c.UserId == userId);

        if (cart == null || cart.Cartitems.Count == 0)
        {
            TempData["SwalIcon"] = "error";
            TempData["SwalTitle"] = "Error";
            TempData["SwalMessage"] = "Your cart is empty.";
            return RedirectToAction("Index", "Cart");
        }

        // เช็คว่าเป็นลูกค้าใหม่หรือไม่ (ยังไม่เคยสั่งซื้อ)
        bool isNewCustomer = !_db.Orders.Any(o => o.UserId == userId);

        // สร้าง Order ใหม่
        var order = new Order();
        order.UserId = userId;
        order.Status = "Pending";
        order.CreatedAt = DateTime.Now;
        _db.Orders.Add(order);
        _db.SaveChanges();

        decimal totalPrice = 0;
        decimal promoDiscount = 0;

        // วนลูปสินค้าในตะกร้า สร้าง OrderItem
        foreach (var ci in cart.Cartitems)
        {
            if (ci.Product == null) continue;

            decimal originalPrice = ci.Product.Price ?? 0;
            string promoName = "";
            decimal discountPct = 0;
            decimal finalPrice = GetDiscountedPriceWithInfo(ci.Product.Id, originalPrice, out promoName, out discountPct);
            int qty = ci.Quantity ?? 0;

            // สร้าง OrderItem
            var orderItem = new Orderitem();
            orderItem.OrderId = order.Id;
            orderItem.ProductId = ci.Product.Id;
            orderItem.Quantity = qty;
            orderItem.Price = finalPrice;
            _db.Orderitems.Add(orderItem);

            totalPrice += originalPrice * qty;
            promoDiscount += (originalPrice - finalPrice) * qty;

            // ลด Stock
            if (ci.Product.Stock != null)
            {
                ci.Product.Stock -= qty;
                if (ci.Product.Stock < 0) ci.Product.Stock = 0;
            }
        }

        // คำนวณส่วนลดลูกค้าใหม่ 5%
        decimal afterPromo = totalPrice - promoDiscount;
        decimal newCustDiscount = 0;
        if (isNewCustomer)
        {
            newCustDiscount = Math.Round(afterPromo * 5m / 100m, 2);
        }
        decimal totalDiscount = promoDiscount + newCustDiscount;

        // อัพเดทราคารวม
        order.TotalPrice = totalPrice;
        order.Discount = totalDiscount;
        order.FinalPrice = totalPrice - totalDiscount;

        // ลบสินค้าในตะกร้า
        _db.Cartitems.RemoveRange(cart.Cartitems);
        _db.SaveChanges();

        return RedirectToAction("Confirmation", new { id = order.Id });
    }

    // หน้ายืนยันคำสั่งซื้อ
    [HttpGet]
    public IActionResult Confirmation(int id)
    {
        int? userId = GetCurrentUserId();
        if (userId == null) return Challenge();

        var order = _db.Orders
            .Include(o => o.Orderitems)
            .ThenInclude(oi => oi.Product)
            .FirstOrDefault(o => o.Id == id && o.UserId == userId);

        if (order == null) return NotFound();

        var viewModel = BuildOrderViewModel(order);
        return View(viewModel);
    }

    // ประวัติคำสั่งซื้อ
    [HttpGet]
    public IActionResult MyOrders()
    {
        int? userId = GetCurrentUserId();
        if (userId == null) return Challenge();

        var orders = _db.Orders
            .Include(o => o.Orderitems)
            .ThenInclude(oi => oi.Product)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToList();

        // สร้าง list ของ ViewModel
        var viewModelList = new List<OrderViewModel>();
        foreach (var order in orders)
        {
            viewModelList.Add(BuildOrderViewModel(order));
        }

        return View(viewModelList);
    }

    // รายละเอียดคำสั่งซื้อ
    [HttpGet]
    public IActionResult Details(int id)
    {
        int? userId = GetCurrentUserId();
        if (userId == null) return Challenge();

        var order = _db.Orders
            .Include(o => o.Orderitems)
            .ThenInclude(oi => oi.Product)
            .FirstOrDefault(o => o.Id == id && o.UserId == userId);

        if (order == null) return NotFound();

        var viewModel = BuildOrderViewModel(order);
        return View(viewModel);
    }

    // ===== Private Helper Methods =====

    // สร้าง OrderViewModel จาก Order
    private OrderViewModel BuildOrderViewModel(Order order)
    {
        var viewModel = new OrderViewModel();
        viewModel.Id = order.Id;
        viewModel.Status = order.Status;
        viewModel.TotalPrice = order.TotalPrice ?? 0;
        viewModel.Discount = order.Discount ?? 0;
        viewModel.FinalPrice = order.FinalPrice ?? 0;
        viewModel.CreatedAt = order.CreatedAt;

        if (order.User != null)
        {
            viewModel.UserName = order.User.Name + " " + order.User.Lastname;
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

        return viewModel;
    }

    // คำนวณราคาหลังส่วนลด พร้อมส่งชื่อโปรโมชั่นกลับมาด้วย
    private decimal GetDiscountedPriceWithInfo(int productId, decimal originalPrice, out string promotionName, out decimal discountPct)
    {
        promotionName = "";
        discountPct = 0;
        var now = DateTime.Now;

        // หา Promotion ที่ใช้งานอยู่
        var promoProduct = _db.Promotionproducts
            .Include(pp => pp.Promotion)
            .FirstOrDefault(pp =>
                pp.ProductId == productId &&
                pp.Promotion != null &&
                pp.Promotion.IsActive == true &&
                pp.Promotion.StartDate <= now &&
                pp.Promotion.EndDate >= now);

        if (promoProduct == null || promoProduct.Promotion == null)
        {
            return originalPrice;
        }

        var promo = promoProduct.Promotion;

        // คำนวณส่วนลดเป็น % (จำกัด 0-100)
        if (promo.DiscountValue != null && promo.DiscountValue > 0)
        {
            decimal percent = promo.DiscountValue.Value;
            if (percent > 100) percent = 100;
            decimal discountAmount = originalPrice * percent / 100;
            decimal result = Math.Round(originalPrice - discountAmount, 2);
            if (result < 0) result = 0;

            promotionName = promo.Name ?? "Promotion";
            discountPct = percent;
            return result;
        }

        return originalPrice;
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
}
