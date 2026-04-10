using System.ComponentModel.DataAnnotations;

namespace CSI_402_Final_Project.ViewModel;

public class PromotionViewModel
{
    public int Id { get; set; }

    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    public string DiscountType { get; set; } = "percentage";

    [Required]
    [Range(0.01, 999999)]
    public decimal DiscountValue { get; set; }

    public decimal? MinOrderAmount { get; set; }

    [Required]
    public DateTime StartDate { get; set; } = DateTime.Now;

    [Required]
    public DateTime EndDate { get; set; } = DateTime.Now.AddDays(30);

    public bool IsActive { get; set; } = true;

    public List<int> SelectedProductIds { get; set; } = new List<int>();
}
