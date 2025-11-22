using System;
using System.Linq;
using System.Threading.Tasks;
using FashionShop.Business.DTOs;
using FashionShop.Business.Interfaces;
using FashionShop.Core.Entities;
using FashionShop.Core.Interfaces;

namespace FashionShop.Business.Services
{
    public class OrderService : IOrderService
    {
        private readonly IRepository<Order> _orderRepository;
        private readonly IRepository<Product> _productRepository;
        private readonly IInventoryService _inventoryService;

        public OrderService(IRepository<Order> orderRepository, IRepository<Product> productRepository, IInventoryService inventoryService)
        {
            _orderRepository = orderRepository;
            _productRepository = productRepository;
            _inventoryService = inventoryService;
        }

        public async Task<OrderDto> PlaceOrderAsync(CreateOrderDto createOrderDto)
        {
            // 0. Input Validation
            if (createOrderDto.Items == null || !createOrderDto.Items.Any())
            {
                throw new ArgumentException("Order must contain at least one item.");
            }

            if (createOrderDto.UserId == null && string.IsNullOrEmpty(createOrderDto.GuestId))
            {
                throw new ArgumentException("Order must be associated with a User or a Guest.");
            }

            // 1. Validate Stock
            foreach (var item in createOrderDto.Items)
            {
                if (!await _inventoryService.CheckStockAsync(item.ProductId, item.Quantity))
                {
                    throw new Exception($"Insufficient stock for product ID {item.ProductId}");
                }
            }

            // 2. Calculate Total and Create Order Items
            decimal totalAmount = 0;
            var orderItems = new System.Collections.Generic.List<OrderItem>();

            foreach (var itemDto in createOrderDto.Items)
            {
                var product = await _productRepository.GetByIdAsync(itemDto.ProductId);
                if (product == null) throw new Exception($"Product {itemDto.ProductId} not found");

                totalAmount += product.Price * itemDto.Quantity;
                orderItems.Add(new OrderItem
                {
                    ProductId = itemDto.ProductId,
                    Quantity = itemDto.Quantity,
                    UnitPrice = product.Price
                });

                // 3. Deduct Stock
                await _inventoryService.DeductStockAsync(itemDto.ProductId, itemDto.Quantity);
            }

            // 4. Create Order
            var order = new Order
            {
                UserId = createOrderDto.UserId,
                GuestId = createOrderDto.GuestId,
                CustomerName = createOrderDto.CustomerName,
                PhoneNumber = createOrderDto.PhoneNumber,
                Address = createOrderDto.Address,
                TotalAmount = totalAmount,
                Status = "Pending",
                OrderItems = orderItems
            };

            await _orderRepository.AddAsync(order);

            return MapToDto(order);
        }

        public async Task<OrderDto?> GetOrderByIdAsync(int orderId)
        {
            var orders = await _orderRepository.ListAsync(o => o.Id == orderId);
            var order = orders.FirstOrDefault();
            
            if (order == null) return null;

            // Manually load related entities
            foreach (var item in order.OrderItems)
            {
                item.Product = await _productRepository.GetByIdAsync(item.ProductId);
            }

            return MapToDto(order);
        }

        public async Task<IReadOnlyList<OrderDto>> GetOrdersByUserIdAsync(int userId)
        {
            var orders = await _orderRepository.ListAsync(o => o.UserId == userId);
            
            // Manually load related entities for each order
            foreach (var order in orders)
            {
                foreach (var item in order.OrderItems)
                {
                    item.Product = await _productRepository.GetByIdAsync(item.ProductId);
                }
            }

            return orders.Select(MapToDto).ToList();
        }

        private static OrderDto MapToDto(Order order)
        {
            return new OrderDto
            {
                Id = order.Id,
                UserId = order.UserId,
                GuestId = order.GuestId,
                CustomerName = order.CustomerName,
                PhoneNumber = order.PhoneNumber,
                Address = order.Address,
                OrderDate = order.OrderDate,
                TotalAmount = order.TotalAmount,
                Status = order.Status,
                Items = order.OrderItems.Select(i => new OrderItemDto
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    ProductName = i.Product?.Name ?? "Unknown" // Note: Product might be null if not included
                }).ToList()
            };
        }
    }
}
