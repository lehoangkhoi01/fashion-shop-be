using System.ComponentModel.DataAnnotations;

namespace FashionShop.Business.DTOs
{
    public class ProductDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public object? Properties { get; set; }
        public int? CatalogId { get; set; }
        public string? CatalogName { get; set; }
    }

    public class CreateProductDto
    {
        [Required(ErrorMessage = "Product Name is required")]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Sku { get; set; } = string.Empty;

        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
        public decimal Price { get; set; }

        public object? Properties { get; set; }
        
        public int? CatalogId { get; set; }
    }

    public class UpdateProductDto : CreateProductDto
    {
        [Required]
        public int Id { get; set; }
    }

}
