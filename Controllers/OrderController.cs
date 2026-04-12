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

        // หาโปรโมชั่น Global ที่ Active อยู่
        var now = DateTime.Now;
        var globalPromo = _db.Promotions
            .FirstOrDefault(p =>
                p.IsActive == true &&
                p.DiscountType == "global" &&
                p.StartDate <= now &&
                p.EndDate >= now);

        // สร้าง ViewModel
        var viewModel = new CheckoutViewModel();
        viewModel.Addresses = addresses;
        viewModel.IsNewCustomer = isNewCustomer;
        if (globalPromo != null)
        {
            viewModel.GlobalPromoName = globalPromo.Name ?? "Festival Promotion";
        }

        decimal subTotal = 0;
        decimal totalProductPromoDiscount = 0;
        decimal totalGlobalPromoDiscount = 0;

        foreach (var ci in cart.Cartitems)
        {
            if (ci.Product == null) continue;

            decimal originalPrice = ci.Product.Price ?? 0;
            int qty = ci.Quantity ?? 0;

            // ขั้นตอน 1: ราคาตั้งต้น
            decimal price = originalPrice;

            // ขั้นตอน 2: ลดจากโปรเฉพาะสินค้า (ถ้ามี)
            string productPromoName = "";
            decimal productPromoPct = 0;
            var promoProduct = _db.Promotionproducts
                .Include(pp => pp.Promotion)
                .FirstOrDefault(pp =>
                    pp.ProductId == ci.Product.Id &&
                    pp.Promotion != null &&
                    pp.Promotion.IsActive == true &&
                    pp.Promotion.DiscountType == "percentage" &&
                    pp.Promotion.StartDate <= now &&
                    pp.Promotion.EndDate >= now);

            decimal priceAfterProductPromo = price;
            if (promoProduct != null && promoProduct.Promotion != null)
            {
                productPromoPct = promoProduct.Promotion.DiscountValue ?? 0;
                if (productPromoPct > 100) productPromoPct = 100;
                if (productPromoPct > 0)
                {
                    productPromoName = promoProduct.Promotion.Name ?? "Promotion";
                    priceAfterProductPromo = Math.Round(price - (price * productPromoPct / 100), 2);
                    if (priceAfterProductPromo < 0) priceAfterProductPromo = 0;
                }
            }

            // ขั้นตอน 3: ลดจากโปรเทศกาล Global (ถ้ามี) - ลดจากราคาหลังขั้นตอน 2
            decimal globalPromoPct = 0;
            string globalPromoName = "";
            decimal priceAfterGlobalPromo = priceAfterProductPromo;
            if (globalPromo != null && globalPromo.DiscountValue != null && globalPromo.DiscountValue > 0)
            {
                globalPromoPct = globalPromo.DiscountValue.Value;
                if (globalPromoPct > 100) globalPromoPct = 100;
                globalPromoName = globalPromo.Name ?? "Festival Promotion";
                priceAfterGlobalPromo = Math.Round(priceAfterProductPromo - (priceAfterProductPromo * globalPromoPct / 100), 2);
                if (priceAfterGlobalPromo < 0) priceAfterGlobalPromo = 0;
            }

            decimal finalPrice = priceAfterGlobalPromo;

            var item = new CheckoutItemViewModel();
            item.ProductId = ci.Product.Id;
            item.ProductName = ci.Product.Name ?? "Unknown";
            item.ImageUrl = ci.Product.ImageUrl;
            item.Quantity = qty;
            item.OriginalPrice = originalPrice;
            item.FinalPrice = finalPrice;
            item.TotalPrice = finalPrice * qty;
            item.HasDiscount = finalPrice < originalPrice;
            // โปรเฉพาะสินค้า
            item.ProductPromoName = productPromoName;
            item.ProductPromoPercent = productPromoPct;
            item.PriceAfterProductPromo = priceAfterProductPromo;
            // โปรเทศกาล
            item.GlobalPromoName = globalPromoName;
            item.GlobalPromoPercent = globalPromoPct;
            item.PriceAfterGlobalPromo = priceAfterGlobalPromo;

            viewModel.Items.Add(item);

            subTotal += originalPrice * qty;
            totalProductPromoDiscount += (originalPrice - priceAfterProductPromo) * qty;
            totalGlobalPromoDiscount += (priceAfterProductPromo - priceAfterGlobalPromo) * qty;
        }

        viewModel.SubTotal = subTotal;
        viewModel.ProductPromoDiscount = totalProductPromoDiscount;
        viewModel.GlobalPromoDiscount = totalGlobalPromoDiscount;

        // ขั้นตอน 4: ลูกค้าใหม่ ลดเพิ่ม 5% จากยอดหลังหักโปรทั้งหมด
        decimal afterAllPromo = subTotal - totalProductPromoDiscount - totalGlobalPromoDiscount;
        decimal newCustDiscount = 0;
        if (isNewCustomer)
        {
            newCustDiscount = Math.Round(afterAllPromo * 5m / 100m, 2);
        }
        viewModel.NewCustomerDiscount = newCustDiscount;
        viewModel.Total = afterAllPromo - newCustDiscount;

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

        // เช็คว่าเป็นลูกค้าใหม่หรือไม่
        bool isNewCustomer = !_db.Orders.Any(o => o.UserId == userId);

        // หาโปรโมชั่น Global ที่ Active
        var now = DateTime.Now;
        var globalPromo = _db.Promotions
            .FirstOrDefault(p =>
                p.IsActive == true &&
                p.DiscountType == "global" &&
                p.StartDate <= now &&
                p.EndDate >= now);

        // สร้าง Order ใหม่
        var order = new Order();
        order.UserId = userId;
        order.Status = "Pending";
        order.CreatedAt = DateTime.Now;
        _db.Orders.Add(order);
        _db.SaveChanges();

        decimal subTotal = 0;
        decimal totalProductPromoDiscount = 0;
        decimal totalGlobalPromoDiscount = 0;

        // วนลูปสินค้าในตะกร้า สร้าง OrderItem
        foreach (var ci in cart.Cartitems)
        {
            if (ci.Product == null) continue;

            decimal originalPrice = ci.Product.Price ?? 0;
            int qty = ci.Quantity ?? 0;

            // ขั้นตอน 1: ราคาตั้งต้น
            decimal price = originalPrice;

            // ขั้นตอน 2: ลดจากโปรเฉพาะสินค้า
            var promoProduct = _db.Promotionproducts
                .Include(pp => pp.Promotion)
                .FirstOrDefault(pp =>
                    pp.ProductId == ci.Product.Id &&
                    pp.Promotion != null &&
                    pp.Promotion.IsActive == true &&
                    pp.Promotion.DiscountType == "percentage" &&
                    pp.Promotion.StartDate <= now &&
                    pp.Promotion.EndDate >= now);

            decimal priceAfterProductPromo = price;
            if (promoProduct != null && promoProduct.Promotion != null)
            {
                decimal pct = promoProduct.Promotion.DiscountValue ?? 0;
                if (pct > 100) pct = 100;
                if (pct > 0)
                {
                    priceAfterProductPromo = Math.Round(price - (price * pct / 100), 2);
                    if (priceAfterProductPromo < 0) priceAfterProductPromo = 0;
                }
            }

            // ขั้นตอน 3: ลดจากโปรเทศกาล Global
            decimal priceAfterGlobalPromo = priceAfterProductPromo;
            if (globalPromo != null && globalPromo.DiscountValue != null && globalPromo.DiscountValue > 0)
            {
                decimal gPct = globalPromo.DiscountValue.Value;
                if (gPct > 100) gPct = 100;
                priceAfterGlobalPromo = Math.Round(priceAfterProductPromo - (priceAfterProductPromo * gPct / 100), 2);
                if (priceAfterGlobalPromo < 0) priceAfterGlobalPromo = 0;
            }

            decimal finalPrice = priceAfterGlobalPromo;

            // สร้าง OrderItem (เก็บราคาหลังลดโปรสินค้า + เทศกาล)
            var orderItem = new Orderitem();
            orderItem.OrderId = order.Id;
            orderItem.ProductId = ci.Product.Id;
            orderItem.Quantity = qty;
            orderItem.Price = finalPrice;
            _db.Orderitems.Add(orderItem);

            subTotal += originalPrice * qty;
            totalProductPromoDiscount += (originalPrice - priceAfterProductPromo) * qty;
            totalGlobalPromoDiscount += (priceAfterProductPromo - priceAfterGlobalPromo) * qty;

            // ลด Stock
            if (ci.Product.Stock != null)
            {
                ci.Product.Stock -= qty;
                if (ci.Product.Stock < 0) ci.Product.Stock = 0;
            }
        }

        // ขั้นตอน 4: ลูกค้าใหม่ ลดเพิ่ม 5%
        decimal afterAllPromo = subTotal - totalProductPromoDiscount - totalGlobalPromoDiscount;
        decimal newCustDiscount = 0;
        if (isNewCustomer)
        {
            newCustDiscount = Math.Round(afterAllPromo * 5m / 100m, 2);
        }
        decimal totalDiscount = totalProductPromoDiscount + totalGlobalPromoDiscount + newCustDiscount;

        // อัพเดทราคารวม
        order.TotalPrice = subTotal;
        order.Discount = totalDiscount;
        order.FinalPrice = subTotal - totalDiscount;

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
