using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FashionShop.Core.Entities
{
    public class Catalog : BaseEntity
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
