using System;
using System.Collections.Generic;

namespace CSI_402_Final_Project.Models;

public partial class Shippingaddress
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public string? Address { get; set; }

    public string? City { get; set; }

    public string? PostalCode { get; set; }

    public virtual User? User { get; set; }
}
