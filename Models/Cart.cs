using System;
using System.Collections.Generic;

namespace CSI_402_Final_Project.Models;

public partial class Cart
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public virtual ICollection<Cartitem> Cartitems { get; set; } = new List<Cartitem>();

    public virtual User? User { get; set; }
}
