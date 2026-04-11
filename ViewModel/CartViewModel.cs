using CSI_402_Final_Project.Models;

namespace CSI_402_Final_Project.ViewModel;

public class CartViewModel
{
    public int CartId { get; set; }
    public List<CartItemViewModel> Items { get; set; } = new List<CartItemViewModel>();
    public decimal TotalPrice { get; set; }
    public int TotalItems { get; set; }
}

public class CartItemViewModel
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public decimal OriginalPrice { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public decimal TotalPrice { get; set; }
    public bool HasDiscount { get; set; }
    public string? ImageUrl { get; set; }
    public int Stock { get; set; }
}
