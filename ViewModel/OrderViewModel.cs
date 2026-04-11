namespace CSI_402_Final_Project.ViewModel;

public class OrderViewModel
{
    public int Id { get; set; }
    public string? Status { get; set; }
    public decimal TotalPrice { get; set; }
    public decimal Discount { get; set; }
    public decimal FinalPrice { get; set; }
    public DateTime? CreatedAt { get; set; }
    public string UserName { get; set; } = "";
    public string UserEmail { get; set; } = "";
    public List<OrderItemViewModel> Items { get; set; } = new List<OrderItemViewModel>();
}

public class OrderItemViewModel
{
    public string ProductName { get; set; } = "";
    public string? ImageUrl { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal Total { get; set; }
}
