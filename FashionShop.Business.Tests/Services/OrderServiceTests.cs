using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FashionShop.Business.DTOs;
using FashionShop.Business.Interfaces;
using FashionShop.Business.Services;
using FashionShop.Core.Entities;
using FashionShop.Core.Interfaces;
using Moq;
using Xunit;

namespace FashionShop.Business.Tests.Services
{
    public class OrderServiceTests
    {
        private readonly Mock<IRepository<Order>> _mockOrderRepository;
        private readonly Mock<IRepository<Product>> _mockProductRepository;
        private readonly Mock<IInventoryService> _mockInventoryService;
        private readonly OrderService _orderService;

        public OrderServiceTests()
        {
            _mockOrderRepository = new Mock<IRepository<Order>>();
            _mockProductRepository = new Mock<IRepository<Product>>();
            _mockInventoryService = new Mock<IInventoryService>();
            _orderService = new OrderService(
                _mockOrderRepository.Object,
                _mockProductRepository.Object,
                _mockInventoryService.Object);
        }

        [Fact]
        public async Task PlaceOrderAsync_ValidOrder_CreatesOrderSuccessfully()
        {
            // Arrange
            var createOrderDto = new CreateOrderDto
            {
                UserId = 1,
                CustomerName = "John Doe",
                PhoneNumber = "1234567890",
                Address = "123 Main St",
                Items = new List<CreateOrderItemDto>
                {
                    new CreateOrderItemDto { ProductId = 1, Quantity = 2 }
                }
            };

            var product = new Product
            {
                Id = 1,
                Name = "Test Product",
                Sku = "SKU001",
                Price = 100
            };

            _mockInventoryService.Setup(s => s.CheckStockAsync(1, 2))
                .ReturnsAsync(true);
            _mockProductRepository.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(product);
            _mockInventoryService.Setup(s => s.DeductStockAsync(1, 2))
                .Returns(Task.CompletedTask);
            _mockOrderRepository.Setup(r => r.AddAsync(It.IsAny<Order>()))
                .Returns((Task<Order>)Task.CompletedTask)
                .Callback<Order>(o => o.Id = 1);

            // Act
            var result = await _orderService.PlaceOrderAsync(createOrderDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("John Doe", result.CustomerName);
            Assert.Equal(200, result.TotalAmount); // 2 * 100
            Assert.Equal("Pending", result.Status);
            _mockOrderRepository.Verify(r => r.AddAsync(It.IsAny<Order>()), Times.Once);
            _mockInventoryService.Verify(s => s.DeductStockAsync(1, 2), Times.Once);
        }

        [Fact]
        public async Task PlaceOrderAsync_EmptyItems_ThrowsArgumentException()
        {
            // Arrange
            var createOrderDto = new CreateOrderDto
            {
                UserId = 1,
                CustomerName = "John Doe",
                PhoneNumber = "1234567890",
                Address = "123 Main St",
                Items = new List<CreateOrderItemDto>()
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _orderService.PlaceOrderAsync(createOrderDto));
            Assert.Contains("at least one item", exception.Message);
        }

        [Fact]
        public async Task PlaceOrderAsync_NoUserIdOrGuestId_ThrowsArgumentException()
        {
            // Arrange
            var createOrderDto = new CreateOrderDto
            {
                CustomerName = "John Doe",
                PhoneNumber = "1234567890",
                Address = "123 Main St",
                Items = new List<CreateOrderItemDto>
                {
                    new CreateOrderItemDto { ProductId = 1, Quantity = 2 }
                }
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _orderService.PlaceOrderAsync(createOrderDto));
            Assert.Contains("User or a Guest", exception.Message);
        }

        [Fact]
        public async Task PlaceOrderAsync_InsufficientStock_ThrowsException()
        {
            // Arrange
            var createOrderDto = new CreateOrderDto
            {
                UserId = 1,
                CustomerName = "John Doe",
                PhoneNumber = "1234567890",
                Address = "123 Main St",
                Items = new List<CreateOrderItemDto>
                {
                    new CreateOrderItemDto { ProductId = 1, Quantity = 100 }
                }
            };

            _mockInventoryService.Setup(s => s.CheckStockAsync(1, 100))
                .ReturnsAsync(false);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(
                () => _orderService.PlaceOrderAsync(createOrderDto));
            Assert.Contains("Insufficient stock", exception.Message);
        }

        [Fact]
        public async Task PlaceOrderAsync_ProductNotFound_ThrowsException()
        {
            // Arrange
            var createOrderDto = new CreateOrderDto
            {
                UserId = 1,
                CustomerName = "John Doe",
                PhoneNumber = "1234567890",
                Address = "123 Main St",
                Items = new List<CreateOrderItemDto>
                {
                    new CreateOrderItemDto { ProductId = 999, Quantity = 1 }
                }
            };

            _mockInventoryService.Setup(s => s.CheckStockAsync(999, 1))
                .ReturnsAsync(true);
            _mockProductRepository.Setup(r => r.GetByIdAsync(999))
                .ReturnsAsync((Product)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(
                () => _orderService.PlaceOrderAsync(createOrderDto));
            Assert.Contains("not found", exception.Message);
        }

        [Fact]
        public async Task PlaceOrderAsync_MultipleItems_CalculatesTotalCorrectly()
        {
            // Arrange
            var createOrderDto = new CreateOrderDto
            {
                UserId = 1,
                CustomerName = "John Doe",
                PhoneNumber = "1234567890",
                Address = "123 Main St",
                Items = new List<CreateOrderItemDto>
                {
                    new CreateOrderItemDto { ProductId = 1, Quantity = 2 },
                    new CreateOrderItemDto { ProductId = 2, Quantity = 3 }
                }
            };

            var product1 = new Product { Id = 1, Name = "Product 1", Price = 100 };
            var product2 = new Product { Id = 2, Name = "Product 2", Price = 50 };

            _mockInventoryService.Setup(s => s.CheckStockAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(true);
            _mockProductRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(product1);
            _mockProductRepository.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(product2);
            _mockInventoryService.Setup(s => s.DeductStockAsync(It.IsAny<int>(), It.IsAny<int>()))
                .Returns(Task.CompletedTask);
            _mockOrderRepository.Setup(r => r.AddAsync(It.IsAny<Order>()))
                .Returns((Task<Order>)Task.CompletedTask);

            // Act
            var result = await _orderService.PlaceOrderAsync(createOrderDto);

            // Assert
            Assert.Equal(350, result.TotalAmount); // (2 * 100) + (3 * 50)
            Assert.Equal(2, result.Items.Count);
        }

        [Fact]
        public async Task GetOrderByIdAsync_WhenOrderExists_ReturnsOrder()
        {
            // Arrange
            var order = new Order
            {
                Id = 1,
                UserId = 1,
                CustomerName = "John Doe",
                PhoneNumber = "1234567890",
                Address = "123 Main St",
                TotalAmount = 200,
                Status = "Pending",
                OrderItems = new List<OrderItem>
                {
                    new OrderItem { ProductId = 1, Quantity = 2, UnitPrice = 100 }
                }
            };

            var product = new Product { Id = 1, Name = "Test Product" };

            _mockOrderRepository.Setup(r => r.ListAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Order, bool>>>()))
                .ReturnsAsync(new List<Order> { order });
            _mockProductRepository.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(product);

            // Act
            var result = await _orderService.GetOrderByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal("John Doe", result.CustomerName);
            Assert.Single(result.Items);
        }

        [Fact]
        public async Task GetOrderByIdAsync_WhenOrderDoesNotExist_ReturnsNull()
        {
            // Arrange
            _mockOrderRepository.Setup(r => r.ListAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Order, bool>>>()))
                .ReturnsAsync(new List<Order>());

            // Act
            var result = await _orderService.GetOrderByIdAsync(999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetOrdersByUserIdAsync_ReturnsUserOrders()
        {
            // Arrange
            var orders = new List<Order>
            {
                new Order
                {
                    Id = 1,
                    UserId = 1,
                    CustomerName = "John Doe",
                    TotalAmount = 200,
                    Status = "Pending",
                    OrderItems = new List<OrderItem>
                    {
                        new OrderItem { ProductId = 1, Quantity = 2, UnitPrice = 100 }
                    }
                },
                new Order
                {
                    Id = 2,
                    UserId = 1,
                    CustomerName = "John Doe",
                    TotalAmount = 150,
                    Status = "Completed",
                    OrderItems = new List<OrderItem>
                    {
                        new OrderItem { ProductId = 2, Quantity = 1, UnitPrice = 150 }
                    }
                }
            };

            var product1 = new Product { Id = 1, Name = "Product 1" };
            var product2 = new Product { Id = 2, Name = "Product 2" };

            _mockOrderRepository.Setup(r => r.ListAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Order, bool>>>()))
                .ReturnsAsync(orders);
            _mockProductRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(product1);
            _mockProductRepository.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(product2);

            // Act
            var result = await _orderService.GetOrdersByUserIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.All(result, order => Assert.Equal(1, order.UserId));
        }

        [Fact]
        public async Task PlaceOrderAsync_GuestOrder_CreatesOrderWithGuestId()
        {
            // Arrange
            var createOrderDto = new CreateOrderDto
            {
                GuestId = "guest-123",
                CustomerName = "Guest User",
                PhoneNumber = "1234567890",
                Address = "123 Main St",
                Items = new List<CreateOrderItemDto>
                {
                    new CreateOrderItemDto { ProductId = 1, Quantity = 1 }
                }
            };

            var product = new Product { Id = 1, Name = "Test Product", Price = 100 };

            _mockInventoryService.Setup(s => s.CheckStockAsync(1, 1)).ReturnsAsync(true);
            _mockProductRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(product);
            _mockInventoryService.Setup(s => s.DeductStockAsync(1, 1)).Returns(Task.CompletedTask);
            _mockOrderRepository.Setup(r => r.AddAsync(It.IsAny<Order>()))
                .Returns((Task<Order>)Task.CompletedTask);

            // Act
            var result = await _orderService.PlaceOrderAsync(createOrderDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("guest-123", result.GuestId);
            Assert.Null(result.UserId);
        }
    }
}
