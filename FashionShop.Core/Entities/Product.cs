using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FashionShop.Core.Entities
{
    public class Product : BaseEntity
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Sku { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        // Stored as JSONB in PostgreSQL
        [Column(TypeName = "jsonb")]
        public string? Properties { get; set; } 

        public int? CatalogId { get; set; }

        [ForeignKey(nameof(CatalogId))]
        public Catalog? Catalog { get; set; }
    }
}
