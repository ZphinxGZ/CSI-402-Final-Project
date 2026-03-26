using System;
using System.Collections.Generic;

namespace CSI_402_Final_Project.Models;

public partial class Promotion
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public string? Description { get; set; }

    public string? DiscountType { get; set; }

    public decimal? DiscountValue { get; set; }

    public decimal? MinOrderAmount { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public bool? IsActive { get; set; }

    public virtual ICollection<Promotionproduct> Promotionproducts { get; set; } = new List<Promotionproduct>();
}
