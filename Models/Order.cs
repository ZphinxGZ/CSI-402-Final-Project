using System;
using System.Collections.Generic;

namespace CSI_402_Final_Project.Models;

public partial class Order
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public decimal? TotalPrice { get; set; }

    public decimal? Discount { get; set; }

    public decimal? FinalPrice { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Orderitem> Orderitems { get; set; } = new List<Orderitem>();

    public virtual User? User { get; set; }
}
