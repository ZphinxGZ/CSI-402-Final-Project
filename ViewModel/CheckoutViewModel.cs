using CSI_402_Final_Project.Models;

namespace CSI_402_Final_Project.ViewModel;

public class CheckoutViewModel
{
    public List<CheckoutItemViewModel> Items { get; set; } = new List<CheckoutItemViewModel>();
    public List<Shippingaddress> Addresses { get; set; } = new List<Shippingaddress>();
    public int? SelectedAddressId { get; set; }
    public decimal SubTotal { get; set; }
    // ส่วนลดรวมจากโปรสินค้าเฉพาะ
    public decimal ProductPromoDiscount { get; set; }
    // ส่วนลดรวมจากโปรเทศกาล (Global)
    public decimal GlobalPromoDiscount { get; set; }
    public string? GlobalPromoName { get; set; }
    // โปรโมชั่นลูกค้าใหม่
    public bool IsNewCustomer { get; set; }
    public decimal NewCustomerDiscount { get; set; }
    public decimal Total { get; set; }
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
    // ขั้นตอนที่ 2: โปรเฉพาะสินค้า
    public string? ProductPromoName { get; set; }
    public decimal ProductPromoPercent { get; set; }
    public decimal PriceAfterProductPromo { get; set; }
    // ขั้นตอนที่ 3: โปรเทศกาล (Global)
    public string? GlobalPromoName { get; set; }
    public decimal GlobalPromoPercent { get; set; }
    public decimal PriceAfterGlobalPromo { get; set; }
    // ขั้นตอนที่ 4: ลูกค้าใหม่ (คำนวณในระดับ Order Summary)
}
