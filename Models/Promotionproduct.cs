using System;
using System.Collections.Generic;

namespace CSI_402_Final_Project.Models;

public partial class Promotionproduct
{
    public int Id { get; set; }

    public int? PromotionId { get; set; }

    public int? ProductId { get; set; }

    public virtual Product? Product { get; set; }

    public virtual Promotion? Promotion { get; set; }
}
