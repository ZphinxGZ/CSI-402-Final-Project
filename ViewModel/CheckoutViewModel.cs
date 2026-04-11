using CSI_402_Final_Project.Models;

namespace CSI_402_Final_Project.ViewModel;

public class CheckoutViewModel
{
    public List<CheckoutItemViewModel> Items { get; set; } = new List<CheckoutItemViewModel>();
    public List<Shippingaddress> Addresses { get; set; } = new List<Shippingaddress>();
    public int? SelectedAddressId { get; set; }
    public decimal SubTotal { get; set; }
    public decimal Discount { get; set; }
    public decimal Total { get; set; }
    // โปรโมชั่นลูกค้าใหม่
    public bool IsNewCustomer { get; set; }
    public decimal NewCustomerDiscount { get; set; }
}

public class CheckoutItemViewModel
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public string? ImageUrl { get; set; }
    public int Quantity { get; set; }
    public decimal OriginalPrice { get; set; }
    public decimal FinalPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public bool HasDiscount { get; set; }
    // ชื่อโปรโมชั่นที่ใช้
    public string? PromotionName { get; set; }
    public decimal DiscountPercent { get; set; }
}
