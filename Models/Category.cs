using System;
using System.Collections.Generic;

namespace CSI_402_Final_Project.Models;

public partial class Category
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
