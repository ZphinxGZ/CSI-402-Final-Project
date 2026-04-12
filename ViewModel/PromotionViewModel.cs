namespace CSI_402_Final_Project.ViewModel;

public class PromotionViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public decimal DiscountValue { get; set; }
    public DateTime StartDate { get; set; } = DateTime.Now;
    public DateTime EndDate { get; set; } = DateTime.Now.AddDays(30);
    public bool IsActive { get; set; } = true;
    // true = ลดทุกสินค้าตามช่วงเวลา, false = เลือกสินค้าเฉพาะ
    public bool IsGlobal { get; set; } = false;
    public List<int> SelectedProductIds { get; set; } = new List<int>();
}
