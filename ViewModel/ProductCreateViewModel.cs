using System.ComponentModel.DataAnnotations;

namespace CSI_402_Final_Project.ViewModel
{
    public class ProductCreateViewModel
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = "";

        [Required]
        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Price { get; set; }

        [Required]
        public int CategoryId { get; set; }

        [StringLength(500)]
        public string? ImageUrl { get; set; }

        public int? Stock { get; set; } = 0;
    }
}
