using System.ComponentModel.DataAnnotations;

namespace CSI_402_Final_Project.ViewModel;

public class ShippingAddressEditViewModel
{
    public int? Id { get; set; }

    [Required]
    public string Address { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(20)]
    public string? PostalCode { get; set; }
}
