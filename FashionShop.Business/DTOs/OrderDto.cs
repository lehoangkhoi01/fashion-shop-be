using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FashionShop.Business.DTOs
{
    public class OrderDto
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public string? GuestId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<OrderItemDto> Items { get; set; } = new();
    }

    public class OrderItemDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public class CreateOrderDto
    {
        public int? UserId { get; set; }
        
        [MaxLength(100)]
        public string? GuestId { get; set; }
        
        [Required(ErrorMessage = "Customer Name is required")]
        [MaxLength(200, ErrorMessage = "Customer Name cannot exceed 200 characters")]
        public string CustomerName { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Phone Number is required")]
        [MaxLength(50, ErrorMessage = "Phone Number cannot exceed 50 characters")]
        public string PhoneNumber { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Address is required")]
        [MaxLength(500, ErrorMessage = "Address cannot exceed 500 characters")]
        public string Address { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Order must contain at least one item")]
        [MinLength(1, ErrorMessage = "Order must contain at least one item")]
        public List<CreateOrderItemDto> Items { get; set; } = new();
    }

    public class CreateOrderItemDto
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Product ID must be greater than 0")]
        public int ProductId { get; set; }
        
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; }
    }
}
