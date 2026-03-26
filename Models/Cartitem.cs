using System;
using System.Collections.Generic;

namespace CSI_402_Final_Project.Models;

public partial class Cartitem
{
    public int Id { get; set; }

    public int? CartId { get; set; }

    public int? ProductId { get; set; }

    public int? Quantity { get; set; }

    public virtual Cart? Cart { get; set; }

    public virtual Product? Product { get; set; }
}
